using Domain.Operations.Enums;
using System;

namespace Application.Operations.MaintenanceTickets.Requests;

public sealed class UpdateMaintenanceTicketRequest
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required MaintenancePriority Priority { get; init; }
    public Guid? DeviceId { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? DeviceEventId { get; init; }
}
