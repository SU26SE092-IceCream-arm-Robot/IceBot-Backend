namespace Application.Abstractions.Realtime.Events;

public sealed record DeviceEventCreatedEvent
{
    public string Type => "DeviceEventCreated";
    public required Guid DeviceEventId { get; init; }
    public required Guid KioskId { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public required string Severity { get; init; }
    public required string EventType { get; init; }
    public required string Message { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}
