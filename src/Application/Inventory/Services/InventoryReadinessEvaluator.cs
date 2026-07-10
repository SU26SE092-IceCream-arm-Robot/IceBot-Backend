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
        CancellationToken cancellationToken = default)
    {
        var kiosk = await inventory.GetKioskForInventoryTopologyAsync(kioskId, cancellationToken);
        if (kiosk is null)
        {
            return null;
        }

        return await EvaluateAsync(kiosk, routes, cancellationToken);
    }

    public async Task<IReadOnlyList<KioskInventoryReadinessResult>> EvaluateOrganizationAsync(
        Guid organizationId,
        IReadOnlyCollection<InventoryReadinessRouteInput> routes,
        CancellationToken cancellationToken = default)
    {
        var kiosks = await inventory.ListKiosksForInventoryReadinessAsync(organizationId, cancellationToken);
        var results = new List<KioskInventoryReadinessResult>(kiosks.Count);
        foreach (var kiosk in kiosks)
        {
            var applicableRoutes = routes.Where(route => AppliesToKiosk(route, kiosk)).ToArray();
            if (applicableRoutes.Length > 0)
            {
                results.Add(await EvaluateAsync(kiosk, applicableRoutes, cancellationToken));
            }
        }

        return results;
    }

    private async Task<KioskInventoryReadinessResult> EvaluateAsync(
        Kiosk kiosk,
        IReadOnlyCollection<InventoryReadinessRouteInput> routes,
        CancellationToken cancellationToken)
    {
        var applicableRoutes = routes.Where(route => AppliesToKiosk(route, kiosk)).ToArray();
        var recipeIds = applicableRoutes.Select(route => route.RecipeId).Distinct().ToArray();
        var recipeItems = await inventory.ListRequiredRecipeItemsAsync(recipeIds, cancellationToken);
        var states = await inventory.ListStatesForInventoryTopologyAsync(kiosk.Id, cancellationToken);
        var statesByIngredient = states.ToLookup(state => state.IngredientId);

        var itemsByRecipe = recipeItems.ToLookup(item => item.RecipeId);
        var results = new List<InventoryIngredientReadinessResult>();
        foreach (var route in applicableRoutes)
        {
            foreach (var item in itemsByRecipe[route.RecipeId])
            {
                var matching = statesByIngredient[item.IngredientId].ToArray();
                results.Add(new InventoryIngredientReadinessResult
                {
                    ExecutionRouteId = route.ExecutionRouteId,
                    RouteCode = route.RouteCode,
                    RecipeId = route.RecipeId,
                    IngredientId = item.IngredientId,
                    IngredientCode = item.Ingredient.Code,
                    IngredientName = item.Ingredient.Name,
                    Status = ResolveStatus(matching),
                    MatchingDispenserStateIds = matching.Select(state => state.Id).ToArray()
                });
            }
        }

        var overallStatus = ResolveOverallStatus(results);
        return new KioskInventoryReadinessResult
        {
            KioskId = kiosk.Id,
            OrganizationId = kiosk.OrganizationId,
            StoreId = kiosk.StoreId,
            IsReady = results.All(item => item.Status == InventoryReadinessStatus.Ready),
            OverallStatus = overallStatus,
            Ingredients = results
        };
    }

    private static InventoryReadinessStatus ResolveStatus(IReadOnlyCollection<IngredientDispenserState> states)
    {
        if (states.Count == 0)
        {
            return InventoryReadinessStatus.MissingIngredient;
        }

        if (states.All(state => !state.Ingredient.IsActive))
        {
            return InventoryReadinessStatus.MissingIngredient;
        }

        var active = states.Where(state => state.IsActive && state.Ingredient.IsActive).ToArray();
        if (active.Length == 0)
        {
            return InventoryReadinessStatus.ContainerInactive;
        }

        var online = active.Where(state => state.Device.Status == DeviceStatus.Online).ToArray();
        if (online.Length == 0)
        {
            return InventoryReadinessStatus.DeviceUnavailable;
        }

        return online.Any(state => !string.IsNullOrWhiteSpace(state.LevelToQuantityProfileJson))
            ? InventoryReadinessStatus.Ready
            : InventoryReadinessStatus.CalibrationMissing;
    }

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
            InventoryReadinessStatus.CalibrationMissing
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
