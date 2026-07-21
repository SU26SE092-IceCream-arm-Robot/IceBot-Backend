namespace Application.Abstractions.Realtime.Events;

public sealed class KioskOperationalStateChangedEvent
{
    public Guid KioskId { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid StoreId { get; init; }
    public required string OldState { get; init; }
    public required string NewState { get; init; }
    public required string Reason { get; init; }
    public Guid ChangedByAccountId { get; init; }
    public Guid? SourceMaintenanceTicketId { get; init; }
    public DateTimeOffset ChangedAt { get; init; }
    public int Version { get; init; } = 1;
}
