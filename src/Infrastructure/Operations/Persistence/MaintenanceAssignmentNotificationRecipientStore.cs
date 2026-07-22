using Application.Operations.Notifications;
using Domain.Identity.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Operations.Persistence;

public sealed class MaintenanceAssignmentNotificationRecipientStore(IceBotDbContext db)
    : IMaintenanceAssignmentNotificationRecipientStore
{
    public Task<bool> CanReceiveAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        db.Accounts.AsNoTracking().AnyAsync(account =>
            account.Id == accountId &&
            account.DeletedAt == null &&
            account.Status == AccountStatus.Active &&
            account.NotificationDevices.Any(device =>
                device.DeletedAt == null && device.InvalidatedAt == null && device.PushToken != null),
            cancellationToken);
}
