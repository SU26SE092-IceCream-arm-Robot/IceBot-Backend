using Application.Abstractions.Realtime;
using Application.Operations.Abstractions;
using Application.Operations.MaintenanceTickets.Commands;
using Application.Operations.MaintenanceTickets.Requests;
using Application.Operations.Notifications;
using Domain.Operations.Entities;
using NSubstitute;

namespace IceBot.UnitTests.Operations;

public sealed class AssignMaintenanceTicketNotificationTests
{
    [Fact]
    public async Task NotificationEnqueueFailure_PreventsTicketSave()
    {
        var ticket = new MaintenanceTicket
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            KioskId = Guid.NewGuid(),
            TicketNumber = "MT-ROLLBACK",
            IssueCode = "TEST",
            Title = "Test",
            ReportedAt = DateTimeOffset.UtcNow
        };
        var store = Substitute.For<IMaintenanceTicketStore>();
        store.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);
        store.CanAssignAccountAsync(
                Arg.Any<Guid>(), ticket.OrganizationId, ticket.StoreId, ticket.KioskId,
                Arg.Any<CancellationToken>())
            .Returns(true);
        var notifier = Substitute.For<IMaintenanceAssignmentNotifier>();
        notifier.NotifyAsync(ticket, Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("outbox unavailable"));
        var handler = new AssignMaintenanceTicketCommandHandler(
            store,
            Substitute.For<IRealtimeNotificationPublisher>(),
            notifier);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(new AssignMaintenanceTicketCommand
        {
            TicketId = ticket.Id,
            UserContext = new() { AccountId = Guid.NewGuid(), IsSystemAdmin = true },
            Request = new AssignMaintenanceTicketRequest { AssignedToAccountId = Guid.NewGuid() }
        }));

        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssigneeOutsideTicketScope_IsRejectedBeforeMutation()
    {
        var ticket = new MaintenanceTicket
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            KioskId = Guid.NewGuid(),
            TicketNumber = "MT-TENANT",
            IssueCode = "TEST",
            Title = "Test",
            ReportedAt = DateTimeOffset.UtcNow
        };
        var store = Substitute.For<IMaintenanceTicketStore>();
        store.GetByIdAsync(ticket.Id, Arg.Any<CancellationToken>()).Returns(ticket);
        store.CanAssignAccountAsync(
                Arg.Any<Guid>(), ticket.OrganizationId, ticket.StoreId, ticket.KioskId,
                Arg.Any<CancellationToken>())
            .Returns(false);
        var notifier = Substitute.For<IMaintenanceAssignmentNotifier>();
        var handler = new AssignMaintenanceTicketCommandHandler(
            store, Substitute.For<IRealtimeNotificationPublisher>(), notifier);

        var result = await handler.HandleAsync(new AssignMaintenanceTicketCommand
        {
            TicketId = ticket.Id,
            UserContext = new() { AccountId = Guid.NewGuid(), IsSystemAdmin = true },
            Request = new AssignMaintenanceTicketRequest { AssignedToAccountId = Guid.NewGuid() }
        });

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Null(ticket.AssignedToAccountId);
        await notifier.DidNotReceive().NotifyAsync(
            Arg.Any<MaintenanceTicket>(), Arg.Any<CancellationToken>());
    }
}
