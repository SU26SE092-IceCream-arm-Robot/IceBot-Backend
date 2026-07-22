using Domain.Devices.ExecutionEndpoints;
using Domain.Devices.Catalog;
using Domain.ProductionExecution.Enums;

namespace Application.Devices.Connectivity.Commands;
public sealed class IngestExecutionReadinessCommand
{
    public required Guid KioskId { get; init; }
    public required Guid EndpointId { get; init; }
    public required Guid SourceExecutorId { get; init; }
    public required long StateRevision { get; init; }
    public required DateTimeOffset ExecutorReportedAt { get; init; }
    public required ExecutionReadinessState Readiness { get; init; }
    public required ExecutionActivityState Activity { get; init; }
    public required ExecutionSafetyState Safety { get; init; }
    public Guid? CurrentCommandId { get; init; }
    public PhysicalOutputState PhysicalOutputState { get; init; } = PhysicalOutputState.Unknown;
    public string? FaultCode { get; init; }
    public IReadOnlyCollection<ExecutionCapabilityInput> Capabilities { get; init; } = [];
}
public sealed record ExecutionCapabilityInput(string CapabilityCode, string? WorkcellCode, bool IsAvailable, string? UnavailableReason);
