using Application.Operations.MaintenanceTickets.Results;
using Domain.Operations.Entities;
using Domain.Operations.Enums;
using System;

namespace Application.Operations.MaintenanceTickets.Mapping;

public static class MaintenanceTicketResultMapper
{
    public static MaintenanceTicketResult ToResult(MaintenanceTicket ticket)
    {
        if (ticket is null)
        {
            throw new ArgumentNullException(nameof(ticket));
        }

        return new MaintenanceTicketResult
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            OrganizationId = ticket.OrganizationId,
            StoreId = ticket.StoreId,
            KioskId = ticket.KioskId,
            DeviceId = ticket.DeviceId,
            OrderId = ticket.OrderId,
            DeviceEventId = ticket.DeviceEventId,
            Title = ticket.Title,
            Description = ticket.Description,
            Priority = ticket.Priority.ToString(),
            Status = ticket.Status.ToString(),
            AssignedToAccountId = ticket.AssignedToAccountId,
            CreatedByAccountId = ticket.CreatedByAccountId,
            ReportedAt = ticket.ReportedAt,
            DueAt = ticket.DueAt,
            AssignedAt = ticket.AssignedAt,
            StartedAt = ticket.StartedAt,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt,
            CancelledAt = ticket.CancelledAt,
            ResolutionNotes = ticket.ResolutionNotes,
            CancelReason = ticket.CancelReason,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt
        };
    }
}
