namespace Application.Abstractions.Realtime.Events;

public sealed record OrderExecutionObservationChangedEvent
{
    public string Type => "OrderExecutionObservationChanged";
    public required Guid OrderId { get; init; }
    public required string OrderNumber { get; init; }
    public required Guid KioskId { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public required string ObservationStatus { get; init; }
    public required string CustomerExecutionStatus { get; init; }
    public required string CustomerStatus { get; init; }
    public required string CustomerStatusMessage { get; init; }
    public required bool RequiresStaffSupport { get; init; }
    public required DateTimeOffset LastExecutorReportedAt { get; init; }
    public required DateTimeOffset LastCloudReceivedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required int Version { get; init; }
}
