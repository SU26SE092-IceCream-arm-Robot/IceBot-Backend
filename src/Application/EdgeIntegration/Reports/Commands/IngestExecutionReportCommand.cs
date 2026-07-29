namespace Application.EdgeIntegration.Reports.Commands;

public sealed class IngestExecutionReportCommand
{
    public required Guid KioskId { get; init; }
    public required Guid EndpointId { get; init; }
    public required Guid CommandId { get; init; }
    public required Guid SourceEventId { get; init; }
    public required long SequenceNumber { get; init; }
    public required DateTimeOffset EdgeCreatedAt { get; init; }
    public DateTimeOffset? ExecutorReportedAt { get; init; }
    public required string ReportType { get; init; }
    public required string Status { get; init; }
    public Guid? DeploymentId { get; init; }
    public Guid? SourceProductionJobId { get; init; }
    public Guid? OrderItemId { get; init; }
    public int? ProductionUnitNo { get; init; }
    public int? ProductionUnitQuantity { get; init; }
    public Guid? WorkcellId { get; init; }
    public Guid? ControllerId { get; init; }
    public string? ExecutionPlanChecksum { get; init; }
    public long? ActiveSetVersion { get; init; }
    public string? ActiveSetChecksum { get; init; }
    public Guid? SourceConfigurationReleaseId { get; init; }
    public string? ReleaseChecksum { get; init; }
    public bool? PhysicalOutputMayHaveOccurred { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string? PayloadJson { get; init; }
    public IReadOnlyCollection<StockMovementEvidenceInput> StockMovements { get; init; } = [];
}

public sealed record StockMovementEvidenceInput(
    Guid SourceEventId,
    Guid IngredientDispenserStateId,
    decimal QuantityConsumed,
    decimal? BalanceAfter,
    DateTimeOffset? OccurredAt,
    bool IsEstimated,
    Guid OrderItemId = default);
