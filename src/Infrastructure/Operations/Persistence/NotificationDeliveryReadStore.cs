using Application.Operations.Notifications.Diagnostics;
using Domain.Operations.Entities;
using Domain.Operations.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Operations.Persistence;

public sealed class NotificationDeliveryReadStore(IceBotDbContext db) : INotificationDeliveryReadStore
{
    public Task<int> CountAsync(Guid organizationId, NotificationDeliveryStatus? status,
        string? notificationType, Guid? recipientAccountId, Guid? kioskId, DateTimeOffset? from,
        DateTimeOffset? to, bool isSystemAdmin, IReadOnlyCollection<Guid> organizationIds,
        IReadOnlyCollection<Guid> storeIds, IReadOnlyCollection<Guid> kioskIds,
        CancellationToken cancellationToken = default) =>
        Filter(organizationId, status, notificationType, recipientAccountId, kioskId, from, to,
            isSystemAdmin, organizationIds, storeIds, kioskIds).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<NotificationDelivery>> ListAsync(Guid organizationId,
        NotificationDeliveryStatus? status, string? notificationType, Guid? recipientAccountId,
        Guid? kioskId, DateTimeOffset? from, DateTimeOffset? to, bool isSystemAdmin,
        IReadOnlyCollection<Guid> organizationIds, IReadOnlyCollection<Guid> storeIds,
        IReadOnlyCollection<Guid> kioskIds, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default) =>
        await Filter(organizationId, status, notificationType, recipientAccountId, kioskId, from, to,
                isSystemAdmin, organizationIds, storeIds, kioskIds)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);

    public Task<NotificationDelivery?> GetAsync(Guid organizationId, Guid deliveryId,
        bool isSystemAdmin, IReadOnlyCollection<Guid> organizationIds,
        IReadOnlyCollection<Guid> storeIds, IReadOnlyCollection<Guid> kioskIds,
        CancellationToken cancellationToken = default) =>
        ApplyScope(db.NotificationDeliveries.AsNoTracking().Where(x =>
                x.OrganizationId == organizationId && x.Id == deliveryId),
            isSystemAdmin, organizationIds, storeIds, kioskIds)
            .SingleOrDefaultAsync(cancellationToken);

    private IQueryable<NotificationDelivery> Filter(Guid organizationId,
        NotificationDeliveryStatus? status, string? notificationType, Guid? recipientAccountId,
        Guid? kioskId, DateTimeOffset? from, DateTimeOffset? to, bool isSystemAdmin,
        IReadOnlyCollection<Guid> organizationIds, IReadOnlyCollection<Guid> storeIds,
        IReadOnlyCollection<Guid> kioskIds)
    {
        var query = db.NotificationDeliveries.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);
        query = ApplyScope(query, isSystemAdmin, organizationIds, storeIds, kioskIds);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(notificationType))
            query = query.Where(x => x.NotificationType == notificationType.Trim());
        if (recipientAccountId.HasValue) query = query.Where(x => x.RecipientAccountId == recipientAccountId);
        if (kioskId.HasValue) query = query.Where(x => x.KioskId == kioskId);
        if (from.HasValue) query = query.Where(x => x.CreatedAt >= from);
        if (to.HasValue) query = query.Where(x => x.CreatedAt <= to);
        return query;
    }

    private static IQueryable<NotificationDelivery> ApplyScope(IQueryable<NotificationDelivery> query,
        bool isSystemAdmin, IReadOnlyCollection<Guid> organizationIds,
        IReadOnlyCollection<Guid> storeIds, IReadOnlyCollection<Guid> kioskIds) =>
        isSystemAdmin
            ? query
            : query.Where(x => organizationIds.Contains(x.OrganizationId) ||
                (x.StoreId.HasValue && storeIds.Contains(x.StoreId.Value)) ||
                (x.KioskId.HasValue && kioskIds.Contains(x.KioskId.Value)));
}
