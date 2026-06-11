using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.RuntimeMenus.Mapping;
using Application.SalesCatalog.RuntimeMenus.Results;
using Application.SalesCatalog.RuntimeMenus.Rules;
using Application.Shared.Wrappers;
using Application.Tenants.Kiosks.Rules;

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
        var menus = await _menus.ListActiveMenusForKioskAsync(
            kiosk.OrganizationId,
            kiosk.StoreId,
            kiosk.Id,
            now,
            cancellationToken);

        var items = menus
            .SelectMany(menu => menu.MenuItems.Select(item => new { Menu = menu, Item = item }))
            .Where(entry => RuntimeMenuSellabilityRules.IsSellable(entry.Item, now))
            .OrderBy(entry => entry.Menu.DisplayOrder)
            .ThenBy(entry => entry.Item.DisplayOrder)
            .ThenBy(entry => entry.Item.DisplayName)
            .Select(entry => RuntimeMenuResultMapper.ToResult(entry.Item))
            .ToList();

        var result = new RuntimeMenuResult
        {
            SnapshotId = Guid.CreateVersion7(),
            KioskId = kiosk.Id,
            GeneratedAt = now,
            ExpiresAt = now.Add(SnapshotTtl),
            ContainsMachineRuntimeState = false,
            Items = items
        };

        return ApiResult<RuntimeMenuResult>.Success(result);
    }
}
