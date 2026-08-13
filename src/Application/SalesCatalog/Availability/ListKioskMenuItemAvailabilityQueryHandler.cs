using Application.SalesCatalog.Abstractions;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.SalesCatalog.Enums;

namespace Application.SalesCatalog.Availability;

public sealed class ListKioskMenuItemAvailabilityQueryHandler(IMenuStore menus)
{
    public async Task<ApiResult<IReadOnlyList<KioskMenuItemAvailabilityResult>>> HandleAsync(
        ListKioskMenuItemAvailabilityQuery query,
        CancellationToken cancellationToken = default)
    {
        var kiosk = await menus.GetKioskByIdAsync(query.KioskId, cancellationToken);
        if (kiosk is null || !ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.MenuItemAvailabilityManage,
                query.UserContext,
                kiosk.OrganizationId,
                kiosk.StoreId,
                kiosk.Id))
        {
            return ApiResult<IReadOnlyList<KioskMenuItemAvailabilityResult>>.Fail("Kiosk not found.", 404);
        }

        var now = DateTimeOffset.UtcNow;
        var menuItems = (await menus.ListMenusForKioskAvailabilityAsync(
                kiosk.OrganizationId, kiosk.StoreId, kiosk.Id, now, cancellationToken))
            .SelectMany(menu => menu.MenuItems.Select(item => new { Menu = menu, Item = item }))
            .Where(entry => string.IsNullOrWhiteSpace(query.Search) ||
                entry.Item.DisplayName.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase) ||
                entry.Item.Code.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var pausedIds = await menus.GetPausedMenuItemIdsAsync(
            kiosk.Id,
            menuItems.Select(entry => entry.Item.Id).ToArray(),
            cancellationToken);

        var results = new List<KioskMenuItemAvailabilityResult>(menuItems.Length);
        foreach (var entry in menuItems)
        {
            var snapshot = await menus.GetKioskMenuItemAvailabilityAsync(
                kiosk.Id, entry.Item.Id, cancellationToken: cancellationToken);
            var state = pausedIds.Contains(entry.Item.Id)
                ? MenuItemOperationalAvailabilityState.Paused
                : MenuItemOperationalAvailabilityState.Available;
            if (query.State.HasValue && query.State.Value != state)
            {
                continue;
            }

            results.Add(new KioskMenuItemAvailabilityResult
            {
                KioskId = kiosk.Id,
                MenuId = entry.Menu.Id,
                MenuItemId = entry.Item.Id,
                DisplayName = entry.Item.DisplayName,
                MenuName = entry.Menu.Name,
                CatalogSellable = entry.Item.IsCurrentlySellable(now),
                State = state,
                ReasonCode = snapshot?.State == MenuItemOperationalAvailabilityState.Paused
                    ? snapshot.ReasonCode
                    : null,
                Reason = snapshot?.State == MenuItemOperationalAvailabilityState.Paused
                    ? snapshot.Reason
                    : null,
                Revision = snapshot?.Revision ?? 0,
                ChangedAt = snapshot?.ChangedAt,
                ChangedByAccountId = snapshot?.ChangedByAccountId
            });
        }

        return ApiResult<IReadOnlyList<KioskMenuItemAvailabilityResult>>.Success(results);
    }
}
