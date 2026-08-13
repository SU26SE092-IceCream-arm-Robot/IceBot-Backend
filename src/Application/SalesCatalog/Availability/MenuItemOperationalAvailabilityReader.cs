using Application.SalesCatalog.Abstractions;

namespace Application.SalesCatalog.Availability;

public sealed class MenuItemOperationalAvailabilityReader(IMenuStore menus)
    : IMenuItemOperationalAvailabilityReader
{
    public Task<IReadOnlySet<Guid>> GetPausedMenuItemIdsAsync(
        Guid kioskId,
        IReadOnlyCollection<Guid> menuItemIds,
        CancellationToken cancellationToken = default) =>
        menus.GetPausedMenuItemIdsAsync(kioskId, menuItemIds, cancellationToken);

    public async Task<bool> IsPausedAsync(
        Guid kioskId,
        Guid menuItemId,
        CancellationToken cancellationToken = default) =>
        (await menus.GetPausedMenuItemIdsAsync(kioskId, [menuItemId], cancellationToken))
        .Contains(menuItemId);
}
