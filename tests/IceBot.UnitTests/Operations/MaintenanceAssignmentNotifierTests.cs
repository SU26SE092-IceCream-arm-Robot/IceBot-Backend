using Application.Operations.Alerts.Notifications;
using Application.Operations.Notifications;
using Domain.Operations.Entities;
using NSubstitute;

namespace IceBot.UnitTests.Operations;

public sealed class MaintenanceAssignmentNotifierTests
{
    [Fact]
    public async Task AssignedEligibleAccount_ReceivesOneDurableNotification()
    {
        var accountId = Guid.NewGuid();
        var ticket = CreateAssignedTicket(accountId);
        var recipients = Substitute.For<IMaintenanceAssignmentNotificationRecipientStore>();
        recipients.CanReceiveAsync(accountId, Arg.Any<CancellationToken>()).Returns(true);
        var deliveries = Substitute.For<INotificationDeliveryStore>();
        var notifier = new MaintenanceAssignmentNotifier(recipients, deliveries);

        await notifier.NotifyAsync(ticket);

        await deliveries.Received(1).AddAsync(
            Arg.Is<NotificationDelivery>(delivery =>
                delivery.SubjectId == ticket.Id &&
                delivery.RecipientAccountId == accountId &&
                delivery.DeliveryKey == $"maintenance-assigned:{ticket.Id:D}:account:{accountId:D}"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignedAccountWithoutActivePushDevice_DoesNotEnqueue()
    {
        var accountId = Guid.NewGuid();
        var ticket = CreateAssignedTicket(accountId);
        var recipients = Substitute.For<IMaintenanceAssignmentNotificationRecipientStore>();
        recipients.CanReceiveAsync(accountId, Arg.Any<CancellationToken>()).Returns(false);
        var deliveries = Substitute.For<INotificationDeliveryStore>();
        var notifier = new MaintenanceAssignmentNotifier(recipients, deliveries);

        await notifier.NotifyAsync(ticket);

        await deliveries.DidNotReceive().AddAsync(
            Arg.Any<NotificationDelivery>(), Arg.Any<CancellationToken>());
    }

    private static MaintenanceTicket CreateAssignedTicket(Guid accountId)
    {
        var ticket = new MaintenanceTicket
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            KioskId = Guid.NewGuid(),
            TicketNumber = "MT-TEST",
            IssueCode = "TEST",
            Title = "Test maintenance assignment",
            ReportedAt = DateTimeOffset.UtcNow
        };
        ticket.Assign(accountId);
        return ticket;
    }
}
