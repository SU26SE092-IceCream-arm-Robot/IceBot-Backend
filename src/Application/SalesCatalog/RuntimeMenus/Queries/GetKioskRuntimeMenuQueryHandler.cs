using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.RuntimeMenus.Mapping;
using Application.SalesCatalog.RuntimeMenus.Results;
using Application.SalesCatalog.RuntimeMenus.Rules;
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

        var salesAvailabilityError = KioskSalesAvailabilityRules.ValidateOnlineSalesAvailability(kiosk);
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

        var routeReadiness = new Dictionary<(Guid ProductVariantId, Guid RecipeId), bool>();
        foreach (var candidate in candidates.Where(candidate => candidate.Item.RecipeId.HasValue))
        {
            var recipeId = candidate.Item.RecipeId!.Value;
            var key = (ProductVariantId: candidate.Item.ProductVariantId, RecipeId: recipeId);
            if (!routeReadiness.ContainsKey(key))
            {
                routeReadiness[key] = await _menus.HasActiveProductionRouteAsync(
                    kiosk.Id,
                    key.ProductVariantId,
                    key.RecipeId,
                    cancellationToken);
            }
        }

        var items = candidates
            .Where(entry =>
            {
                var hasActiveProductionRoute = entry.Item.RecipeId.HasValue &&
                                               routeReadiness.GetValueOrDefault((entry.Item.ProductVariantId, entry.Item.RecipeId.Value));
                return RuntimeMenuSellabilityRules.IsSellable(entry.Item, now, hasActiveProductionRoute) &&
                       ProductOptionSelectionRules.IsSatisfiable(optionsByMenuItem[entry.Item.Id].ToArray());
            })
            .OrderBy(entry => entry.Menu.DisplayOrder)
            .ThenBy(entry => entry.Item.DisplayOrder)
            .ThenBy(entry => entry.Item.DisplayName)
            .Select(entry => RuntimeMenuResultMapper.ToResult(entry.Item, optionsByMenuItem[entry.Item.Id].ToArray()))
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
