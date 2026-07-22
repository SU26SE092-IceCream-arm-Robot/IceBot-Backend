namespace Application.Abstractions.Realtime.Events;

public sealed record KioskStatusChangedEvent
{
    public string Type => "KioskStatusChanged";
    public required Guid KioskId { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public string? OldLifecycleStatus { get; init; }
    public string? NewLifecycleStatus { get; init; }
    public string? OldConnectivity { get; init; }
    public string? NewConnectivity { get; init; }
    public string? Reason { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required int Version { get; init; }
}
