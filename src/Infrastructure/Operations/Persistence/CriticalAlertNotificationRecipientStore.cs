using Application.Operations.Alerts.Notifications;
using Domain.Identity.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Operations.Persistence;

public sealed class CriticalAlertNotificationRecipientStore(IceBotDbContext db)
    : IOperationalAlertNotificationRecipientStore
{
    public async Task<IReadOnlyCollection<Guid>> ListRecipientAccountIdsAsync(
        Guid organizationId,
        Guid storeId,
        Guid kioskId,
        CancellationToken cancellationToken = default)
    {
        var eligibleRoles = db.AccountRoles.AsNoTracking()
            .Where(accountRole =>
                accountRole.IsActive &&
                accountRole.Account.Status == AccountStatus.Active &&
                accountRole.Account.DeletedAt == null &&
                accountRole.Account.NotificationDevices.Any(device =>
                    device.DeletedAt == null &&
                    device.InvalidatedAt == null &&
                    device.PushToken != null));

        var primary = await eligibleRoles
            .Where(accountRole =>
                (accountRole.Role.Code == "Technician" &&
                 (accountRole.KioskId == kioskId || accountRole.StoreId == storeId)) ||
                (accountRole.Role.Code == "Manager" &&
                 (accountRole.StoreId == storeId || accountRole.OrganizationId == organizationId)))
            .Select(accountRole => accountRole.AccountId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        if (primary.Length > 0)
        {
            return primary;
        }

        return await eligibleRoles
            .Where(accountRole =>
                accountRole.Role.Code == "OrgAdmin" &&
                accountRole.OrganizationId == organizationId)
            .Select(accountRole => accountRole.AccountId)
            .Distinct()
            .ToArrayAsync(cancellationToken);
    }
}
