using System.Linq.Expressions;
using Application.SalesCatalog.Availability;
using Domain.SalesCatalog.Entities;
using Domain.SalesCatalog.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SalesCatalog.Persistence;

public sealed partial class MenuStore
{
    public Task<List<Menu>> ListMenusForKioskAvailabilityAsync(Guid? organizationId, Guid storeId, Guid kioskId, DateTimeOffset now, CancellationToken cancellationToken = default) =>
        ListActiveMenusForKioskAsync(organizationId, storeId, kioskId, now, cancellationToken);

    public async Task<KioskMenuItemAvailabilitySnapshot?> GetKioskMenuItemAvailabilityAsync(Guid kioskId, Guid menuItemId, bool asNoTracking = true, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.KioskMenuItemAvailabilities.Where(x => x.KioskId == kioskId && x.MenuItemId == menuItemId);
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.Select(ToSnapshot()).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<KioskMenuItemAvailabilityRequestReplay?> GetKioskMenuItemAvailabilityByRequestIdAsync(Guid kioskId, Guid menuItemId, string requestId, CancellationToken cancellationToken = default) =>
        (from transition in _dbContext.KioskMenuItemAvailabilityTransitions.AsNoTracking()
         join availability in _dbContext.KioskMenuItemAvailabilities.AsNoTracking() on transition.AvailabilityId equals availability.Id
         where transition.KioskId == kioskId && transition.MenuItemId == menuItemId && transition.RequestId == requestId
         select new KioskMenuItemAvailabilityRequestReplay(
             transition.KioskId, transition.MenuId, transition.MenuItemId, transition.ToState, transition.ReasonCode,
             transition.Reason, transition.AvailabilityRevision, transition.OccurredAt, transition.ActorAccountId))
        .FirstOrDefaultAsync(cancellationToken);

    public Task<KioskMenuItemAvailability?> GetTrackedKioskMenuItemAvailabilityAsync(Guid kioskId, Guid menuItemId, CancellationToken cancellationToken = default) =>
        _dbContext.KioskMenuItemAvailabilities.FirstOrDefaultAsync(x => x.KioskId == kioskId && x.MenuItemId == menuItemId, cancellationToken);

    public Task AddKioskMenuItemAvailabilityAsync(KioskMenuItemAvailability availability, CancellationToken cancellationToken = default) =>
        _dbContext.KioskMenuItemAvailabilities.AddAsync(availability, cancellationToken).AsTask();

    public async Task<IReadOnlySet<Guid>> GetPausedMenuItemIdsAsync(Guid kioskId, IReadOnlyCollection<Guid> menuItemIds, CancellationToken cancellationToken = default)
    {
        if (menuItemIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        return await _dbContext.KioskMenuItemAvailabilities.AsNoTracking()
            .Where(x => x.KioskId == kioskId && menuItemIds.Contains(x.MenuItemId) && x.State == MenuItemOperationalAvailabilityState.Paused)
            .Select(x => x.MenuItemId)
            .ToHashSetAsync(cancellationToken);
    }

    private static Expression<Func<KioskMenuItemAvailability, KioskMenuItemAvailabilitySnapshot>> ToSnapshot() =>
        availability => new KioskMenuItemAvailabilitySnapshot(
            availability.KioskId,
            availability.MenuId,
            availability.MenuItemId,
            availability.State,
            availability.Revision,
            availability.ReasonCode,
            availability.Reason,
            availability.ChangedAt,
            availability.ChangedByAccountId);
}
