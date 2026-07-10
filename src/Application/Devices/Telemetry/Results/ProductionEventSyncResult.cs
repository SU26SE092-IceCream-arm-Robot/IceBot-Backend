namespace Application.Devices.Telemetry.Results;

public sealed class ProductionEventSyncResult
{
    public Guid EventId { get; init; }
    public bool Duplicate { get; init; }
    public long AcknowledgedSequenceNumber { get; init; }
}

public sealed class ProductionEventCheckpointResult
{
    public Guid SourceExecutorId { get; init; }
    public long LastContiguousSequenceNumber { get; init; }
    public Guid? LastContiguousEventId { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
