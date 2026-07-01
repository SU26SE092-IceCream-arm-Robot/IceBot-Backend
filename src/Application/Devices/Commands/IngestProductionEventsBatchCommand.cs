namespace Application.Devices.Commands;

public sealed class IngestProductionEventsBatchCommand
{
    public required Guid KioskId { get; init; }
    public required Guid EndpointId { get; init; }
    public required Guid SourceExecutorId { get; init; }
    public required IReadOnlyList<ProductionEventBatchItem> Events { get; init; }
}

public sealed class ProductionEventBatchItem
{
    public Guid EventId { get; init; }
    public long SequenceNumber { get; init; }
    public required string EventType { get; init; }
    public int SchemaVersion { get; init; } = 1;
    public DateTimeOffset EdgeCreatedAt { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? SourceCommandId { get; init; }
    public Guid? ProductionJobId { get; init; }
    public string? PayloadJson { get; init; }
}
