namespace Application.Orders.Management.Results;

public sealed class ExecutionAttemptSummaryResult
{
    public Guid SourceCommandId { get; init; }
    public int DispatchAttemptNo { get; init; }
    public string CommandStatus { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? DeliveredAt { get; init; }
    public DateTimeOffset? RespondedAt { get; init; }
    public string? RejectionCode { get; init; }
    public string? RejectionMessage { get; init; }
    public string? ExecutionStatus { get; init; }
    public string? ObservationStatus { get; init; }
    public string? CustomerExecutionStatus { get; init; }
}

public sealed class ExecutionAttemptDiagnosticsResult
{
    public Guid SourceCommandId { get; init; }
    public Guid OrderId { get; init; }
    public int DispatchAttemptNo { get; init; }
    public Guid KioskExecutionEndpointId { get; init; }
    public string CommandStatus { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
    public Guid? RequestedByAccountId { get; init; }
    public DateTimeOffset? CommandExpiryAt { get; init; }
    public DateTimeOffset? DeliveredAt { get; init; }
    public DateTimeOffset? RespondedAt { get; init; }
    public string? RejectionCode { get; init; }
    public string? RejectionMessage { get; init; }
    public string? ExecutionProfile { get; init; }
    public Guid? SourceConfigurationReleaseId { get; init; }
    public string? ReleaseChecksum { get; init; }
    public string? ExecutionStatus { get; init; }
    public string? ObservationStatus { get; init; }
    public string? CustomerExecutionStatus { get; init; }
    public Guid? SourceExecutorId { get; init; }
    public Guid? LastAppliedSourceEventId { get; init; }
    public long? LastAppliedSequenceNumber { get; init; }
    public DateTimeOffset? LastEdgeCreatedAt { get; init; }
    public DateTimeOffset? LastExecutorReportedAt { get; init; }
    public DateTimeOffset? CloudReceivedAt { get; init; }
}

public sealed class ExecutionAttemptDetailResult
{
    public required ExecutionAttemptDiagnosticsResult Attempt { get; init; }
    public ExecutionAttemptReferenceResult? PreviousAttempt { get; init; }
    public ExecutionAttemptReferenceResult? NextAttempt { get; init; }
    public required ExecutionAttemptProvenanceResult Provenance { get; init; }
    public IReadOnlyCollection<ExecutionDeliveryAttemptResult> DeliveryAttempts { get; init; } = [];
    public IReadOnlyCollection<ProductionExecutionResult> ProductionExecutions { get; init; } = [];
    public IReadOnlyCollection<ProductionUnitOutcomeSummaryResult> ProductionUnitOutcomes { get; init; } = [];
}

public sealed class ProductionUnitOutcomeSummaryResult
{
    public Guid OrderItemId { get; init; }
    public int ProductionUnitStartNo { get; init; }
    public int ExpectedQuantity { get; init; }
    public int CompletedQuantity { get; init; }
    public int FailedQuantity { get; init; }
    public int ManualInterventionQuantity { get; init; }
    public int InProgressQuantity { get; init; }
    public int UnreportedQuantity { get; init; }
    public string? AggregateStatus { get; init; }
}

public sealed class ExecutionAttemptReferenceResult
{
    public Guid SourceCommandId { get; init; }
    public int DispatchAttemptNo { get; init; }
    public string CommandStatus { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class ExecutionAttemptProvenanceResult
{
    public bool IsRedispatch { get; init; }
    public Guid? RetryOfSourceCommandId { get; init; }
    public Guid? RequestedByAccountId { get; init; }
    public string? RedispatchReason { get; init; }
    public bool TimedOutBeforeAcceptance { get; init; }
    public DateTimeOffset? TimedOutAt { get; init; }
    public DateTimeOffset? CommandExpiryAt { get; init; }
    public bool ExecutionReportTimedOut { get; init; }
    public DateTimeOffset? ObservationRecordedAt { get; init; }
}

public sealed class ExecutionDeliveryAttemptResult
{
    public int DeliveryAttemptNo { get; init; }
    public DateTimeOffset SentAt { get; init; }
    public string Outcome { get; init; } = null!;
    public string? ResponseCode { get; init; }
    public string? ResponseMessage { get; init; }
}

public sealed class ProductionExecutionResult
{
    public Guid Id { get; init; }
    public Guid? SourceProductionJobId { get; init; }
    public Guid OrderItemId { get; init; }
    public int ProductionUnitNo { get; init; }
    public int ProductionUnitQuantity { get; init; }
    public Guid? WorkcellId { get; init; }
    public Guid? ControllerId { get; init; }
    public string? ExecutionPlanChecksum { get; init; }
    public long? ActiveSetVersion { get; init; }
    public string? ActiveSetChecksum { get; init; }
    public string Status { get; init; } = null!;
    public string PhysicalOutputState { get; init; } = null!;
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid SourceExecutorId { get; init; }
    public Guid LastAppliedSourceEventId { get; init; }
    public long LastAppliedSequenceNumber { get; init; }
    public DateTimeOffset LastEdgeCreatedAt { get; init; }
    public DateTimeOffset LastExecutorReportedAt { get; init; }
    public DateTimeOffset CloudReceivedAt { get; init; }
}
