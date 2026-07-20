using System.Text.Json;
using Application.Operations.Alerts.Notifications;
using Domain.Operations.Entities;

namespace Application.Operations.Notifications;

public interface IMaintenanceAssignmentNotificationRecipientStore
{
    Task<bool> CanReceiveAsync(Guid accountId, CancellationToken cancellationToken = default);
}

public interface IMaintenanceAssignmentNotifier
{
    Task NotifyAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default);
}

public sealed class MaintenanceAssignmentNotifier(
    IMaintenanceAssignmentNotificationRecipientStore recipients,
    INotificationDeliveryStore deliveries) : IMaintenanceAssignmentNotifier
{
    public async Task NotifyAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default)
    {
        if (!ticket.AssignedToAccountId.HasValue ||
            !await recipients.CanReceiveAsync(ticket.AssignedToAccountId.Value, cancellationToken))
            return;

        var data = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["type"] = "maintenance_assigned",
            ["deliveryId"] = string.Empty,
            ["maintenanceTicketId"] = ticket.Id.ToString("D"),
            ["kioskId"] = ticket.KioskId.ToString("D")
        });
        await deliveries.AddAsync(NotificationDelivery.CreatePush(
            ticket.OrganizationId,
            ticket.StoreId,
            ticket.KioskId,
            ticket.Id,
            $"maintenance-assigned:{ticket.Id:D}:account:{ticket.AssignedToAccountId.Value:D}",
            "maintenance_assigned",
            ticket.AssignedToAccountId.Value,
            "Maintenance ticket assigned",
            $"Ticket {ticket.TicketNumber} has been assigned to you.",
            data,
            ticket.AssignedAt ?? DateTimeOffset.UtcNow), cancellationToken);
    }
}
