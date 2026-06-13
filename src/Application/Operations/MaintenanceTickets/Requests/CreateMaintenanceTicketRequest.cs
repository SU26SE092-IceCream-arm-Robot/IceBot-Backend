using Domain.Operations.Enums;
using System;

namespace Application.Operations.MaintenanceTickets.Requests;

public sealed class CreateMaintenanceTicketRequest
{
    public Guid OrganizationId { get; init; }
    public Guid StoreId { get; init; }
    public Guid KioskId { get; init; }
    public Guid? DeviceId { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? DeviceEventId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? IssueCode { get; init; }
    public MaintenancePriority? Priority { get; init; }
}
