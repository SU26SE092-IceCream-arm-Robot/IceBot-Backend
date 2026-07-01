namespace Application.Devices.Commands;

public sealed class IngestProductionEventCommand
{
    public required Guid KioskId { get; init; }
    public required Guid EndpointId { get; init; }
    public required Guid SourceExecutorId { get; init; }
    public required Guid EventId { get; init; }
    public required long SequenceNumber { get; init; }
    public required string EventType { get; init; }
    public required int SchemaVersion { get; init; }
    public required DateTimeOffset EdgeCreatedAt { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? SourceCommandId { get; init; }
    public Guid? ProductionJobId { get; init; }
    public string? PayloadJson { get; init; }
}
