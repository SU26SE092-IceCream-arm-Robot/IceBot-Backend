using Application.Orders.Management.Automation;
using Domain.Catalog.Enums;
using Domain.Identity.Enums;
using Domain.Orders.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Orders.Persistence;

public sealed class FulfillmentReminderStore(IceBotDbContext db) : IFulfillmentReminderStore
{
    public async Task<IReadOnlyList<Guid>> ListOverdueItemIdsAsync(
        DateTimeOffset observedAt,
        int batchSize,
        CancellationToken cancellationToken = default) =>
        await Overdue(observedAt)
            .OrderBy(item => item.Order.PaidAt)
            .ThenBy(item => item.Id)
            .Select(item => item.Id)
            .Take(Math.Clamp(batchSize, 1, 500))
            .ToListAsync(cancellationToken);

    public Task<FulfillmentReminderCandidate?> GetOverdueCandidateAsync(
        Guid orderItemId,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default) =>
        Overdue(observedAt)
            .Where(item => item.Id == orderItemId)
            .Select(item => new FulfillmentReminderCandidate(
                item.Id,
                item.OrderId,
                item.Order.OrderNumber,
                item.Order.Kiosk.OrganizationId,
                item.Order.Kiosk.StoreId,
                item.Order.KioskId,
                item.Order.PaidAt!.Value,
                item.Order.PaidAt.Value.AddSeconds(
                    item.MenuItem.PreparationTimeSeconds ??
                    item.ProductVariant.PreparationTimeSeconds ??
                    item.Product.PreparationTimeSeconds ?? 0)))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Guid>> ListRecipientAccountIdsAsync(
        Guid organizationId,
        Guid storeId,
        Guid kioskId,
        CancellationToken cancellationToken = default)
    {
        var eligible = db.AccountRoles.AsNoTracking().Where(accountRole =>
            accountRole.IsActive &&
            accountRole.Account.Status == AccountStatus.Active &&
            accountRole.Account.DeletedAt == null &&
            accountRole.Account.NotificationDevices.Any(device =>
                device.DeletedAt == null && device.InvalidatedAt == null && device.PushToken != null));

        var primary = await eligible
            .Where(accountRole =>
                (accountRole.Role.Code == "Staff" &&
                 (accountRole.KioskId == kioskId || accountRole.StoreId == storeId)) ||
                (accountRole.Role.Code == "Manager" &&
                 (accountRole.StoreId == storeId || accountRole.OrganizationId == organizationId)))
            .Select(accountRole => accountRole.AccountId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
        if (primary.Length > 0) return primary;

        return await eligible
            .Where(accountRole =>
                accountRole.Role.Code == "OrgAdmin" && accountRole.OrganizationId == organizationId)
            .Select(accountRole => accountRole.AccountId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
    }

    private IQueryable<Domain.Orders.Entities.OrderItem> Overdue(DateTimeOffset observedAt) =>
        db.OrderItems.AsNoTracking().Where(item =>
            item.Order.DeletedAt == null &&
            item.Order.PaymentStatus == PaymentStatus.Paid &&
            item.Order.PaidAt.HasValue &&
            item.FulfillmentType != FulfillmentType.MachineProduced &&
            item.Status != OrderItemStatus.Completed &&
            item.Status != OrderItemStatus.Cancelled &&
            item.Status != OrderItemStatus.Failed &&
            item.Order.Status != OrderStatus.Cancelled &&
            item.Order.Status != OrderStatus.Refunded &&
            item.Order.Status != OrderStatus.Compensated &&
            !db.NotificationDeliveries.Any(delivery =>
                delivery.NotificationType == "fulfillment_overdue" && delivery.SubjectId == item.Id) &&
            db.AccountRoles.Any(accountRole =>
                accountRole.IsActive &&
                accountRole.Account.Status == AccountStatus.Active &&
                accountRole.Account.DeletedAt == null &&
                accountRole.Account.NotificationDevices.Any(device =>
                    device.DeletedAt == null && device.InvalidatedAt == null && device.PushToken != null) &&
                ((accountRole.Role.Code == "Staff" &&
                  (accountRole.KioskId == item.Order.KioskId ||
                   accountRole.StoreId == item.Order.Kiosk.StoreId)) ||
                 (accountRole.Role.Code == "Manager" &&
                  (accountRole.StoreId == item.Order.Kiosk.StoreId ||
                   accountRole.OrganizationId == item.Order.Kiosk.OrganizationId)) ||
                 (accountRole.Role.Code == "OrgAdmin" &&
                  accountRole.OrganizationId == item.Order.Kiosk.OrganizationId))) &&
            (item.MenuItem.PreparationTimeSeconds ??
             item.ProductVariant.PreparationTimeSeconds ??
             item.Product.PreparationTimeSeconds) > 0 &&
            item.Order.PaidAt.Value.AddSeconds(
                item.MenuItem.PreparationTimeSeconds ??
                item.ProductVariant.PreparationTimeSeconds ??
                item.Product.PreparationTimeSeconds ?? 0) <= observedAt);
}
