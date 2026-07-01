namespace Application.Sync.Results;

public sealed class SyncDeadLetterResult
{
    public Guid Id { get; init; }
    public Guid? EventId { get; init; }
    public Guid? KioskId { get; init; }
    public string? KioskCode { get; init; }
    public required string EventType { get; init; }
    public string? AggregateType { get; init; }
    public Guid? AggregateId { get; init; }
    public required string Status { get; init; }
    public int ProcessingAttempts { get; init; }
    public required string ErrorMessage { get; init; }
    public DateTimeOffset FailedAt { get; init; }
    public DateTimeOffset? ResolvedAt { get; init; }
    public string? ResolutionNotes { get; init; }
    public IReadOnlyList<SyncDeadLetterRetryAttemptResult> RetryAttempts { get; init; } = [];
}

public sealed class SyncDeadLetterRetryAttemptResult
{
    public int AttemptNumber { get; init; }
    public Guid RequestedByAccountId { get; init; }
    public DateTimeOffset RequestedAt { get; init; }
    public required string Reason { get; init; }
    public bool? Succeeded { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string? ResultMessage { get; init; }
}
