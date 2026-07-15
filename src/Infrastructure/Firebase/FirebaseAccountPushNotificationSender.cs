using Application.Identity.NotificationDevices.Abstractions;
using Application.Identity.NotificationDevices.Delivery;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

namespace Infrastructure.Firebase;

public sealed class FirebaseAccountPushNotificationSender : IAccountPushNotificationSender
{
    public const string MeterName = "IceBot.Identity.Notifications";
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> Attempted = Meter.CreateCounter<long>("icebot.notification.push.attempted");
    private static readonly Counter<long> Succeeded = Meter.CreateCounter<long>("icebot.notification.push.succeeded");
    private static readonly Counter<long> Failed = Meter.CreateCounter<long>("icebot.notification.push.failed");
    private static readonly Counter<long> Invalidated = Meter.CreateCounter<long>("icebot.notification.push.token_invalidated");
    private readonly IFirebaseClient _firebaseClient;
    private readonly IAccountNotificationDeviceStore _devices;
    private readonly ILogger<FirebaseAccountPushNotificationSender> _logger;

    public FirebaseAccountPushNotificationSender(
        IFirebaseClient firebaseClient,
        IAccountNotificationDeviceStore devices,
        ILogger<FirebaseAccountPushNotificationSender> logger)
    {
        _firebaseClient = firebaseClient;
        _devices = devices;
        _logger = logger;
    }

    public async Task<AccountPushDeliveryResult> SendAsync(
        AccountPushNotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (notification.AccountId == Guid.Empty ||
            string.IsNullOrWhiteSpace(notification.Title) ||
            string.IsNullOrWhiteSpace(notification.Body))
        {
            throw new ArgumentException("Account, title, and body are required.", nameof(notification));
        }

        var devices = await _devices.ListActiveByAccountIdAsync(notification.AccountId, cancellationToken);
        Attempted.Add(devices.Count);
        var succeeded = 0;
        var failed = 0;
        var invalidated = 0;

        foreach (var device in devices)
        {
            try
            {
                await _firebaseClient.GetMessaging().SendAsync(new Message
                {
                    Token = device.PushToken,
                    Notification = new Notification
                    {
                        Title = notification.Title.Trim(),
                        Body = notification.Body.Trim()
                    },
                    Data = notification.Data is null
                        ? null
                        : new Dictionary<string, string>(notification.Data)
                }, cancellationToken);
                succeeded++;
                Succeeded.Add(1);
            }
            catch (FirebaseMessagingException exception) when (IsPermanentTokenFailure(exception))
            {
                device.Invalidate(exception.MessagingErrorCode!.Value.ToString(), DateTimeOffset.UtcNow);
                invalidated++;
                failed++;
                Invalidated.Add(1);
                Failed.Add(1);
                _logger.LogInformation(
                    "Invalidated Firebase token for account {AccountId}, installation {InstallationId}: {ErrorCode}",
                    notification.AccountId,
                    device.InstallationId,
                    exception.MessagingErrorCode);
            }
            catch (FirebaseMessagingException exception)
            {
                failed++;
                Failed.Add(1);
                _logger.LogWarning(
                    exception,
                    "Firebase delivery failed for account {AccountId}, installation {InstallationId}",
                    notification.AccountId,
                    device.InstallationId);
            }
        }

        if (invalidated > 0)
        {
            await _devices.SaveChangesAsync(cancellationToken);
        }

        return new AccountPushDeliveryResult(devices.Count, succeeded, failed, invalidated);
    }

    private static bool IsPermanentTokenFailure(FirebaseMessagingException exception) =>
        exception.MessagingErrorCode is MessagingErrorCode.Unregistered or MessagingErrorCode.SenderIdMismatch;
}
