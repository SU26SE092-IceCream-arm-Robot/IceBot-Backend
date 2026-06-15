namespace Application.Abstractions.Realtime.Events;

public sealed record KioskStatusChangedEvent
{
    public string Type => "KioskStatusChanged";
    public required Guid KioskId { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public required string OldStatus { get; init; }
    public required string NewStatus { get; init; }
    public required string Connectivity { get; init; }
    public string? Reason { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required int Version { get; init; }
}
