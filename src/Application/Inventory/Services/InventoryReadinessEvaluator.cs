using Application.Inventory.Abstractions;
using Application.Inventory.Results;
using Domain.Devices.Catalog;
using Domain.Inventory.Entities;
using Domain.Inventory.Enums;
using Domain.Tenants.Entities;

namespace Application.Inventory.Services;

public sealed class InventoryReadinessEvaluator(IInventoryStore inventory) : IInventoryReadinessEvaluator
{
    public async Task<KioskInventoryReadinessResult?> EvaluateKioskAsync(
        Guid kioskId,
        IReadOnlyCollection<InventoryReadinessRouteInput> routes,
        CancellationToken cancellationToken = default,
        InventoryReadinessEvaluationOptions? options = null)
    {
        var kiosk = await inventory.GetKioskForInventoryTopologyAsync(kioskId, cancellationToken);
        if (kiosk is null)
        {
            return null;
        }

        return await EvaluateAsync(kiosk, routes, options ?? new(), cancellationToken);
    }

    public async Task<IReadOnlyList<KioskInventoryReadinessResult>> EvaluateOrganizationAsync(
        Guid organizationId,
        IReadOnlyCollection<InventoryReadinessRouteInput> routes,
        CancellationToken cancellationToken = default,
        InventoryReadinessEvaluationOptions? options = null)
    {
        var kiosks = await inventory.ListKiosksForInventoryReadinessAsync(organizationId, cancellationToken);
        var results = new List<KioskInventoryReadinessResult>(kiosks.Count);
        foreach (var kiosk in kiosks)
        {
            var applicableRoutes = routes.Where(route => AppliesToKiosk(route, kiosk)).ToArray();
            if (applicableRoutes.Length > 0)
            {
                results.Add(await EvaluateAsync(kiosk, applicableRoutes, options ?? new(), cancellationToken));
            }
        }

        return results;
    }

    private async Task<KioskInventoryReadinessResult> EvaluateAsync(
        Kiosk kiosk,
        IReadOnlyCollection<InventoryReadinessRouteInput> routes,
        InventoryReadinessEvaluationOptions options,
        CancellationToken cancellationToken)
    {
        var applicableRoutes = routes.Where(route => AppliesToKiosk(route, kiosk)).ToArray();
        var recipeIds = applicableRoutes.Select(route => route.RecipeId).Distinct().ToArray();
        var recipeItems = await inventory.ListRequiredRecipeItemsAsync(recipeIds, cancellationToken);
        var supportedOptions = await inventory.ListSupportedProductOptionsAsync(
            applicableRoutes.Select(route => route.ProductId).Distinct().ToArray(),
            applicableRoutes.SelectMany(route => route.SupportedOptionCodes).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            cancellationToken);
        var states = await inventory.ListStatesForInventoryTopologyAsync(kiosk.Id, cancellationToken);
        var balances = await inventory.ListKioskIngredientInventoriesAsync(kiosk.Id, cancellationToken);
        var balancesByIngredient = balances.ToLookup(balance => balance.IngredientId);
        var statesByBalanceId = states.Where(state => state.KioskIngredientInventoryId.HasValue)
            .ToLookup(state => state.KioskIngredientInventoryId!.Value);
        var warnings = BuildSensorAssistedWarnings(balances, statesByBalanceId, options);

        var itemsByRecipe = recipeItems.ToLookup(item => item.RecipeId);
        var results = new List<InventoryIngredientReadinessResult>();
        var optionGroupResults = new List<InventoryOptionGroupReadinessResult>();
        foreach (var route in applicableRoutes)
        {
            foreach (var item in itemsByRecipe[route.RecipeId])
            {
                var matching = balancesByIngredient[item.IngredientId].ToArray();
                results.Add(CreateIngredientResult(
                    route,
                    item.IngredientId,
                    item.Ingredient.Code,
                    item.Ingredient.Name,
                    matching, statesByBalanceId,
                    options,
                    item.Quantity * route.RequestedQuantity,
                    item.Unit,
                    includeRequirementInResult: true));
            }

            foreach (var requirement in route.SelectedOptionIngredients ?? [])
            {
                var matching = balancesByIngredient[requirement.IngredientId].ToArray();
                results.Add(CreateIngredientResult(
                    route,
                    requirement.IngredientId,
                    requirement.IngredientCode,
                    requirement.IngredientName,
                    matching, statesByBalanceId,
                    options,
                    requirement.Quantity * route.RequestedQuantity,
                    requirement.Unit,
                    includeRequirementInResult: true));
            }

            var routeOptions = supportedOptions.Where(option =>
                option.OptionGroup.ProductId == route.ProductId && route.SupportedOptionCodes.Contains(option.Code)).ToArray();
            foreach (var group in routeOptions.GroupBy(option => option.OptionGroupId))
            {
                var groupDefinition = group.First().OptionGroup;
                var optionResults = group.Select(option =>
                {
                    var ingredients = option.IngredientRequirements.Select(requirement =>
                    {
                        var matching = balancesByIngredient[requirement.IngredientId].ToArray();
                        return CreateIngredientResult(
                            route,
                            requirement.IngredientId,
                            requirement.Ingredient.Code,
                            requirement.Ingredient.Name,
                            matching, statesByBalanceId,
                            options,
                            requirement.Quantity,
                            requirement.Unit,
                            includeRequirementInResult: false);
                    }).ToArray();
                    return new InventoryOptionReadinessResult
                    {
                        ProductOptionId = option.Id,
                        OptionCode = option.Code,
                        IsReady = ingredients.All(item => item.Status == InventoryReadinessStatus.Ready),
                        Ingredients = ingredients
                    };
                }).ToArray();
                var readyCount = optionResults.Count(option => option.IsReady);
                optionGroupResults.Add(new InventoryOptionGroupReadinessResult
                {
                    ExecutionRouteId = route.ExecutionRouteId,
                    RouteCode = route.RouteCode,
                    RecipeId = route.RecipeId,
                    OptionGroupId = groupDefinition.Id,
                    OptionGroupCode = groupDefinition.Code,
                    IsRequired = groupDefinition.IsRequired,
                    MinimumSelections = groupDefinition.MinSelections,
                    ReadyOptionCount = readyCount,
                    IsReady = !groupDefinition.IsRequired || readyCount >= groupDefinition.MinSelections,
                    Options = optionResults
                });
            }
        }

        var blockingResults = results.Concat(optionGroupResults.Where(group => group.IsRequired)
            .SelectMany(group => group.Options.Where(option => !option.IsReady).SelectMany(option => option.Ingredients))).ToArray();
        var baseReady = results.All(item => item.Status == InventoryReadinessStatus.Ready);
        var requiredOptionsReady = optionGroupResults.Where(group => group.IsRequired).All(group => group.IsReady);
        var overallStatus = baseReady && requiredOptionsReady
            ? InventoryReadinessStatus.Ready
            : ResolveOverallStatus(blockingResults);
        return new KioskInventoryReadinessResult
        {
            KioskId = kiosk.Id,
            OrganizationId = kiosk.OrganizationId,
            StoreId = kiosk.StoreId,
            HasConfiguredInventoryTopology = states.Count != 0,
            HasConfiguredInventoryBalance = balances.Count != 0,
            IsReady = baseReady && requiredOptionsReady,
            OverallStatus = overallStatus,
            Ingredients = results,
            OptionGroups = optionGroupResults,
            Warnings = warnings
        };
    }

    private static InventoryIngredientReadinessResult CreateIngredientResult(
        InventoryReadinessRouteInput route,
        Guid ingredientId,
        string ingredientCode,
        string ingredientName,
        IReadOnlyCollection<KioskIngredientInventory> matching,
        ILookup<Guid, IngredientDispenserState> statesByBalanceId,
        InventoryReadinessEvaluationOptions options,
        decimal requiredQuantity,
        string requiredUnit,
        bool includeRequirementInResult) =>
        new()
        {
            ExecutionRouteId = route.ExecutionRouteId,
            RouteCode = route.RouteCode,
            RecipeId = route.RecipeId,
            IngredientId = ingredientId,
            IngredientCode = ingredientCode,
            IngredientName = ingredientName,
            RequiredQuantity = includeRequirementInResult && options.Purpose == InventoryReadinessEvaluationPurpose.RuntimeSellability
                ? requiredQuantity
                : null,
            RequiredUnit = includeRequirementInResult && options.Purpose == InventoryReadinessEvaluationPurpose.RuntimeSellability
                ? requiredUnit
                : null,
            Status = ResolveStatus(matching, statesByBalanceId, options, requiredQuantity, requiredUnit),
            MatchingDispenserStateIds = matching.SelectMany(balance => statesByBalanceId[balance.Id]).Select(state => state.Id).ToArray()
        };

    private static InventoryReadinessStatus ResolveStatus(
        IReadOnlyCollection<KioskIngredientInventory> balances,
        ILookup<Guid, IngredientDispenserState> statesByBalanceId,
        InventoryReadinessEvaluationOptions options,
        decimal requiredQuantity,
        string requiredUnit)
    {
        if (balances.Count == 0)
        {
            return InventoryReadinessStatus.MissingIngredient;
        }

        if (balances.All(balance => !balance.Ingredient.IsActive))
        {
            return InventoryReadinessStatus.MissingIngredient;
        }

        var active = balances.Where(balance => balance.IsActive && balance.Ingredient.IsActive).ToArray();
        if (active.Length == 0)
        {
            return InventoryReadinessStatus.ContainerInactive;
        }

        if (options.Purpose == InventoryReadinessEvaluationPurpose.TopologyValidation)
        {
            return InventoryReadinessStatus.Ready;
        }

        var observedAt = options.ObservedAt ?? DateTimeOffset.UtcNow;
        var usable = active.Where(balance =>
            !balance.ExpiresAt.HasValue || balance.ExpiresAt.Value >= observedAt).ToArray();
        if (usable.Length == 0)
        {
            return InventoryReadinessStatus.IngredientExpired;
        }

        var sameUnit = usable.Where(balance =>
            string.Equals(balance.Unit, requiredUnit, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (sameUnit.Length == 0)
        {
            return InventoryReadinessStatus.UnitMismatch;
        }

        var sensorEligible = sameUnit.Where(balance =>
            balance.TrackingMode != InventoryTrackingMode.SensorRequired ||
            AreSensorRequiredStatesReady(statesByBalanceId[balance.Id].ToArray(), observedAt, options.MaximumSensorEvidenceAge)).ToArray();
        if (sensorEligible.Length == 0)
        {
            var strictSensorStates = sameUnit.Where(balance => balance.TrackingMode == InventoryTrackingMode.SensorRequired)
                .SelectMany(balance => statesByBalanceId[balance.Id]).ToArray();
            if (strictSensorStates.Length > 0 && strictSensorStates.All(state => state.Device.Status != DeviceStatus.Online))
            {
                return InventoryReadinessStatus.DeviceUnavailable;
            }

            if (strictSensorStates.Where(state => state.Device.Status == DeviceStatus.Online)
                .All(state => string.IsNullOrWhiteSpace(state.LevelToQuantityProfileJson)))
            {
                return InventoryReadinessStatus.CalibrationMissing;
            }

            return InventoryReadinessStatus.InventoryEvidenceStale;
        }

        if (sensorEligible.Any(balance => !balance.EstimatedQuantity.HasValue))
        {
            return InventoryReadinessStatus.QuantityUnavailable;
        }

        return sensorEligible.Sum(balance => balance.EstimatedQuantity!.Value) >= requiredQuantity
            ? InventoryReadinessStatus.Ready
            : InventoryReadinessStatus.QuantityInsufficient;
    }

    private static IReadOnlyList<string> BuildSensorAssistedWarnings(
        IReadOnlyCollection<KioskIngredientInventory> balances,
        ILookup<Guid, IngredientDispenserState> statesByBalanceId,
        InventoryReadinessEvaluationOptions options)
    {
        var observedAt = options.ObservedAt ?? DateTimeOffset.UtcNow;
        return balances.Where(balance => balance.TrackingMode == InventoryTrackingMode.SensorAssisted)
            .Select(balance =>
            {
                var states = statesByBalanceId[balance.Id].ToArray();
                return AreSensorRequiredStatesReady(states, observedAt, options.MaximumSensorEvidenceAge)
                    ? null
                    : $"Sensor evidence for {balance.Ingredient.Code} is unavailable or stale; the manual balance remains in effect.";
            })
            .Where(warning => warning is not null)
            .Select(warning => warning!)
            .ToArray();
    }

    private static bool AreSensorRequiredStatesReady(
        IReadOnlyCollection<IngredientDispenserState> states,
        DateTimeOffset observedAt,
        TimeSpan? maximumSensorEvidenceAge) =>
        states.Count > 0 && states.All(state =>
            state.Device.Status == DeviceStatus.Online &&
            !string.IsNullOrWhiteSpace(state.LevelToQuantityProfileJson) &&
            maximumSensorEvidenceAge.HasValue &&
            state.LastSensorObservedAt.HasValue &&
            state.LastSensorObservedAt.Value >= observedAt - maximumSensorEvidenceAge.Value);

    private static InventoryReadinessStatus ResolveOverallStatus(
        IReadOnlyCollection<InventoryIngredientReadinessResult> ingredients)
    {
        if (ingredients.All(item => item.Status == InventoryReadinessStatus.Ready))
        {
            return InventoryReadinessStatus.Ready;
        }

        var precedence = new[]
        {
            InventoryReadinessStatus.MissingIngredient,
            InventoryReadinessStatus.ContainerInactive,
            InventoryReadinessStatus.DeviceUnavailable,
            InventoryReadinessStatus.CalibrationMissing,
            InventoryReadinessStatus.IngredientExpired,
            InventoryReadinessStatus.InventoryEvidenceStale,
            InventoryReadinessStatus.UnitMismatch,
            InventoryReadinessStatus.QuantityUnavailable,
            InventoryReadinessStatus.QuantityInsufficient
        };
        return precedence.First(status => ingredients.Any(item => item.Status == status));
    }

    private static bool AppliesToKiosk(InventoryReadinessRouteInput route, Kiosk kiosk) =>
        Applies(route.ProductOrganizationId, route.ProductStoreId, route.ProductKioskId, kiosk) &&
        Applies(route.RecipeOrganizationId, route.RecipeStoreId, route.RecipeKioskId, kiosk);

    private static bool Applies(Guid? organizationId, Guid? storeId, Guid? kioskId, Kiosk kiosk) =>
        (!organizationId.HasValue || organizationId == kiosk.OrganizationId) &&
        (!storeId.HasValue || storeId == kiosk.StoreId) &&
        (!kioskId.HasValue || kioskId == kiosk.Id);
}
