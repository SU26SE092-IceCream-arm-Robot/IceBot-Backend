using Application.Operations.Alerts.Notifications;
using Domain.Operations.Entities;
using NSubstitute;
using System.Text.Json;

namespace IceBot.UnitTests.Operations;

public sealed class CriticalOperationalAlertNotifierTests
{
    [Fact]
    public async Task NotifyAsync_DeduplicatesRecipientsAndUsesBoundedPayload()
    {
        var accountId = Guid.NewGuid();
        var recipients = Substitute.For<IOperationalAlertNotificationRecipientStore>();
        recipients.ListRecipientAccountIdsAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([accountId, accountId]);
        var deliveries = Substitute.For<INotificationDeliveryStore>();
        var notifier = new CriticalOperationalAlertNotifier(recipients, deliveries);

        var notification = CreateNotification();
        await notifier.NotifyAsync(notification);

        await deliveries.Received(1).AddAsync(
            Arg.Is<NotificationDelivery>(delivery =>
                delivery.RecipientAccountId == accountId &&
                delivery.DeliveryKey == $"critical-alert:{notification.AlertId:D}:account:{accountId:D}" &&
                HasExpectedPayload(delivery.DataJson, notification.AlertId)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyAsync_EnqueueFailureEscapesSoAlertTransactionCanRollback()
    {
        var recipients = Substitute.For<IOperationalAlertNotificationRecipientStore>();
        recipients.ListRecipientAccountIdsAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([Guid.NewGuid()]);
        var deliveries = Substitute.For<INotificationDeliveryStore>();
        deliveries.AddAsync(Arg.Any<NotificationDelivery>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("database unavailable"));
        var notifier = new CriticalOperationalAlertNotifier(recipients, deliveries);

        await Assert.ThrowsAsync<InvalidOperationException>(() => notifier.NotifyAsync(CreateNotification()));
    }

    private static bool HasExpectedPayload(string json, Guid alertId)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        return data is not null &&
               data["type"] == "critical_operational_alert" &&
               data["alertId"] == alertId.ToString("D") &&
               !data.ContainsKey("message");
    }

    private static CriticalOperationalAlertNotification CreateNotification() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "MOTOR_OVERHEAT",
        "Robot arm: motor overheat");
}
