namespace Application.Abstractions.Realtime.Events;

public sealed record AlertChangedEvent
{
    public string Type => "AlertChanged";
    public required Guid AlertId { get; init; }
    public required Guid KioskId { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? DeviceId { get; init; }
    public required string AlertCode { get; init; }
    public required string Severity { get; init; }
    public string? OldStatus { get; init; }
    public required string NewStatus { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required int Version { get; init; }
}
