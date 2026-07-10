using Application.EdgeIntegration.CommandDelivery.Commands;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.EdgeIntegration.Reports.Commands;
using Application.EdgeIntegration.Timeouts.Commands;

namespace Application.EdgeIntegration.Reports.Contracts;

public sealed record ExecutionReportInboxPayload
{
    public int SchemaVersion { get; init; } = 1;
    public Guid CommandId { get; init; }
    public string ReportType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public long SequenceNumber { get; init; }
    public DateTimeOffset EdgeCreatedAt { get; init; }
    public DateTimeOffset? ExecutorReportedAt { get; init; }
    public Guid? DeploymentId { get; init; }
    public Guid? SourceProductionJobId { get; init; }
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
    public IReadOnlyList<StockMovementEvidenceInput> StockMovements { get; init; } = [];
}
