using System;

namespace Application.Abstractions.Realtime.Events;

public sealed record MaintenanceTicketChangedEvent
{
    public string Type => "MaintenanceTicketChanged";
    public required Guid TicketId { get; init; }
    public required string TicketNumber { get; init; }
    public required Guid KioskId { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public string? OldStatus { get; init; }
    public required string NewStatus { get; init; }
    public required string Priority { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required int Version { get; init; }
}
