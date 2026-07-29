using System.Text.Json;

namespace Application.Operations.Alerts.Notifications;

public sealed record CriticalOperationalAlertNotification(
    Guid AlertId,
    Guid OrganizationId,
    Guid StoreId,
    Guid KioskId,
    Guid DeviceId,
    string AlertCode,
    string Title);

public interface ICriticalOperationalAlertNotifier
{
    Task NotifyAsync(
        CriticalOperationalAlertNotification notification,
        CancellationToken cancellationToken = default);
}

public interface IOperationalAlertNotificationRecipientStore
{
    Task<IReadOnlyCollection<Guid>> ListRecipientAccountIdsAsync(
        Guid organizationId,
        Guid storeId,
        Guid kioskId,
        CancellationToken cancellationToken = default);
}

public sealed class CriticalOperationalAlertNotifier(
    IOperationalAlertNotificationRecipientStore recipients,
    INotificationDeliveryStore deliveries) : ICriticalOperationalAlertNotifier
{
    public async Task NotifyAsync(
        CriticalOperationalAlertNotification notification,
        CancellationToken cancellationToken = default)
    {
        var accountIds = await recipients.ListRecipientAccountIdsAsync(
            notification.OrganizationId,
            notification.StoreId,
            notification.KioskId,
            cancellationToken);

        foreach (var accountId in accountIds.Distinct())
        {
            var data = new Dictionary<string, string>
            {
                ["type"] = "critical_operational_alert",
                ["deliveryId"] = string.Empty,
                ["alertId"] = notification.AlertId.ToString("D"),
                ["kioskId"] = notification.KioskId.ToString("D"),
                ["deviceId"] = notification.DeviceId.ToString("D"),
                ["alertCode"] = notification.AlertCode,
                ["severity"] = "Critical"
            };

            var delivery = Domain.Operations.Entities.NotificationDelivery.CreatePush(
                notification.OrganizationId,
                notification.StoreId,
                notification.KioskId,
                notification.AlertId,
                $"critical-alert:{notification.AlertId:D}:account:{accountId:D}",
                "critical_operational_alert",
                accountId,
                "Critical kiosk alert",
                notification.Title,
                JsonSerializer.Serialize(data),
                DateTimeOffset.UtcNow);
            await deliveries.AddAsync(delivery, cancellationToken);
        }
    }
}

public interface IInventoryOperationalAlertNotifier
{
    Task NotifyEmptyAsync(
        Guid alertId,
        Guid organizationId,
        Guid storeId,
        Guid kioskId,
        Guid deviceId,
        string title,
        CancellationToken cancellationToken = default);
}

public sealed class InventoryOperationalAlertNotifier(
    IOperationalAlertNotificationRecipientStore recipients,
    INotificationDeliveryStore deliveries) : IInventoryOperationalAlertNotifier
{
    public async Task NotifyEmptyAsync(
        Guid alertId,
        Guid organizationId,
        Guid storeId,
        Guid kioskId,
        Guid deviceId,
        string title,
        CancellationToken cancellationToken = default)
    {
        var accountIds = await recipients.ListRecipientAccountIdsAsync(
            organizationId, storeId, kioskId, cancellationToken);
        foreach (var accountId in accountIds.Distinct())
        {
            var data = new Dictionary<string, string>
            {
                ["type"] = "inventory_empty",
                ["deliveryId"] = string.Empty,
                ["alertId"] = alertId.ToString("D"),
                ["kioskId"] = kioskId.ToString("D"),
                ["deviceId"] = deviceId.ToString("D"),
                ["alertCode"] = "INVENTORY_EMPTY",
                ["severity"] = "Error"
            };
            var delivery = Domain.Operations.Entities.NotificationDelivery.CreatePush(
                organizationId,
                storeId,
                kioskId,
                alertId,
                $"inventory-empty:{alertId:D}:account:{accountId:D}",
                "inventory_empty",
                accountId,
                "Kiosk inventory empty",
                title,
                JsonSerializer.Serialize(data),
                DateTimeOffset.UtcNow);
            await deliveries.AddAsync(delivery, cancellationToken);
        }
    }
}
