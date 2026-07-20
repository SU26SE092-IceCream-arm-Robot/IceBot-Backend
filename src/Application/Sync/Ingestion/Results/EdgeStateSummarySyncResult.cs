namespace Application.Sync.Ingestion.Results;

public sealed class EdgeStateSummarySyncResult
{
    public int AppliedCount { get; init; }
    public int DuplicateCount { get; init; }
    public int StaleCount { get; init; }
    public int RejectedCount { get; init; }
    public required IReadOnlyList<EdgeStateSummarySyncItemResult> Items { get; init; }
}

public sealed class EdgeStateSummarySyncItemResult
{
    public required string SummaryKind { get; init; }
    public long StateRevision { get; init; }
    public required string Status { get; init; }
    public int StatusCode { get; init; }
    public string? Message { get; init; }
}
