namespace Application.Identity.NotificationDevices.Delivery;

public interface IAccountPushNotificationSender
{
    Task<AccountPushDeliveryResult> SendAsync(
        AccountPushNotification notification,
        CancellationToken cancellationToken = default);
}

public sealed record AccountPushNotification(
    Guid AccountId,
    string Title,
    string Body,
    IReadOnlyDictionary<string, string>? Data = null);

public sealed record AccountPushDeliveryResult(
    int TargetCount,
    int SucceededCount,
    int FailedCount,
    int InvalidatedTokenCount);
