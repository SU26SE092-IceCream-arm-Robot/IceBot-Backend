namespace Application.Devices.Results;

public sealed class BatchEventSyncResult
{
    public int AcceptedCount { get; init; }
    public int DuplicateCount { get; init; }
    public int RejectedCount { get; init; }
    public required IReadOnlyList<BatchEventSyncItemResult> Items { get; init; }
}

public sealed class BatchEventSyncItemResult
{
    public required Guid EventId { get; init; }
    public required string EventType { get; init; }
    public required string Status { get; init; }
    public int StatusCode { get; init; }
    public string? Message { get; init; }
    public Guid? ResourceId { get; init; }
    public long? AcknowledgedSequenceNumber { get; init; }
}
