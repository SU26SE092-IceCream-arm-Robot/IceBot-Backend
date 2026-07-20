using Application.Operations.Alerts.Notifications;

namespace IceBot.IntegrationTests.Infrastructure;

internal sealed class RecordingCriticalOperationalAlertNotifier : ICriticalOperationalAlertNotifier
{
    public List<CriticalOperationalAlertNotification> Notifications { get; } = [];

    public Task NotifyAsync(
        CriticalOperationalAlertNotification notification,
        CancellationToken cancellationToken = default)
    {
        Notifications.Add(notification);
        return Task.CompletedTask;
    }
}
