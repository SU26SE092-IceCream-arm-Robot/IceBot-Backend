using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.RuntimeMenus.Mapping;
using Application.SalesCatalog.RuntimeMenus.Results;
using Application.SalesCatalog.ReadModels;
using Application.Shared.Wrappers;
using Application.Tenants.Kiosks.Rules;
using Application.Tenants.Stores;
using Application.SalesCatalog.Rules;

namespace Application.SalesCatalog.RuntimeMenus.Queries;

public sealed class GetKioskRuntimeMenuQueryHandler
{
    private static readonly TimeSpan SnapshotTtl = TimeSpan.FromSeconds(15);
    private readonly IMenuStore _menus;

    public GetKioskRuntimeMenuQueryHandler(IMenuStore menus)
    {
        _menus = menus;
    }

    public async Task<ApiResult<RuntimeMenuResult>> HandleAsync(
        GetKioskRuntimeMenuQuery query,
        CancellationToken cancellationToken = default)
    {
        var kioskId = query.KioskId;
        var kiosk = await _menus.GetKioskByIdAsync(kioskId, cancellationToken);
        if (kiosk is null)
        {
            return ApiResult<RuntimeMenuResult>.Fail("Kiosk not found.", 404);
        }

        var connectivity = await _menus.GetKioskConnectivityAsync(kioskId, cancellationToken);
        var salesAvailabilityError = KioskSalesAvailabilityRules.ValidateOnlineSalesAvailability(kiosk, connectivity);
        if (salesAvailabilityError is not null)
        {
            return ApiResult<RuntimeMenuResult>.Fail(salesAvailabilityError, 409);
        }

        var now = DateTimeOffset.UtcNow;
        var openingHoursError = StoreSalesAvailabilityRules.ValidateOpeningHours(kiosk.Store, now);
        if (openingHoursError is not null)
        {
            return ApiResult<RuntimeMenuResult>.Fail(openingHoursError, 409);
        }

        var menus = await _menus.ListActiveMenusForKioskAsync(
            kiosk.OrganizationId,
            kiosk.StoreId,
            kiosk.Id,
            now,
            cancellationToken);

        var candidates = menus
            .SelectMany(menu => menu.MenuItems.Select(item => new { Menu = menu, Item = item }))
            .ToList();

        var optionRows = await _menus.ListMenuItemProductOptionsAsync(
            candidates.Select(candidate => candidate.Item.Id).Distinct().ToArray(),
            cancellationToken);
        var optionsByMenuItem = optionRows.ToLookup(option => option.MenuItemId);
        var optionGroupRows = await _menus.ListMenuItemOptionGroupsAsync(
            candidates.Select(candidate => candidate.Item.Id).Distinct().ToArray(),
            cancellationToken);
        var optionGroupsByMenuItem = optionGroupRows.ToLookup(group => group.MenuItemId);

        var routePolicies = new Dictionary<(Guid ProductVariantId, Guid RecipeId),
            ActiveProductionRouteOptionPolicy?>();
        foreach (var candidate in candidates.Where(candidate =>
                     candidate.Item.ProductVariant.FulfillmentType == Domain.Catalog.Enums.FulfillmentType.MachineProduced &&
                     candidate.Item.RecipeId.HasValue))
        {
            var recipeId = candidate.Item.RecipeId!.Value;
            var key = (ProductVariantId: candidate.Item.ProductVariantId, RecipeId: recipeId);
            if (!routePolicies.ContainsKey(key))
            {
                routePolicies[key] = await _menus.GetActiveProductionRouteOptionPolicyAsync(
                    kiosk.Id,
                    key.ProductVariantId,
                    key.RecipeId,
                    cancellationToken);
            }
        }

        var filteredOptionsByMenuItem = candidates.ToDictionary(candidate => candidate.Item.Id, candidate =>
        {
            var options = optionsByMenuItem[candidate.Item.Id].ToArray();
            return candidate.Item.ProductVariant.FulfillmentType switch
            {
                Domain.Catalog.Enums.FulfillmentType.Packaged => options.Where(option =>
                    option.ExecutionImpact == Domain.Catalog.Enums.ProductOptionExecutionImpact.CommercialOnly).ToArray(),
                Domain.Catalog.Enums.FulfillmentType.Manual => options,
                Domain.Catalog.Enums.FulfillmentType.MachineProduced when candidate.Item.RecipeId.HasValue =>
                    FilterMachineProducedOptions(candidate.Item.ProductVariantId, candidate.Item.RecipeId.Value, options),
                _ => []
            };

            MenuItemProductOptionReadModel[] FilterMachineProducedOptions(
                Guid productVariantId,
                Guid recipeId,
                MenuItemProductOptionReadModel[] sourceOptions)
            {
                var policy = routePolicies.GetValueOrDefault((productVariantId, recipeId));
                if (policy is null) return [];
                return sourceOptions.Where(option =>
                    option.ExecutionImpact != Domain.Catalog.Enums.ProductOptionExecutionImpact.ProductionAffecting ||
                    policy.SupportedOptionCodes.Contains(option.Code)).ToArray();
            }
        });

        var items = candidates
            .Where(entry =>
            {
                var hasActiveProductionRoute =
                    entry.Item.ProductVariant.FulfillmentType == Domain.Catalog.Enums.FulfillmentType.MachineProduced &&
                    entry.Item.RecipeId.HasValue &&
                    routePolicies.GetValueOrDefault((entry.Item.ProductVariantId, entry.Item.RecipeId.Value)) is not null;
                return MenuItemSellabilityRules.Validate(
                           entry.Item,
                           kiosk,
                           now,
                           hasActiveProductionRoute) is null &&
                       ProductOptionSelectionRules.IsSatisfiable(
                           optionGroupsByMenuItem[entry.Item.Id].ToArray(),
                           filteredOptionsByMenuItem[entry.Item.Id]);
            })
            .OrderBy(entry => entry.Menu.DisplayOrder)
            .ThenBy(entry => entry.Item.DisplayOrder)
            .ThenBy(entry => entry.Item.DisplayName)
            .Select(entry => RuntimeMenuResultMapper.ToResult(entry.Item, filteredOptionsByMenuItem[entry.Item.Id]))
            .ToList();

        var result = new RuntimeMenuResult
        {
            SnapshotId = Guid.CreateVersion7(),
            KioskId = kiosk.Id,
            GeneratedAt = now,
            ExpiresAt = now.Add(SnapshotTtl),
            Items = items
        };

        return ApiResult<RuntimeMenuResult>.Success(result);
    }
}
