using Domain.Common;
using Domain.Devices.ExecutionEndpoints;
using Domain.ProductionExecution.Enums;
using Domain.Tenants.Entities;

namespace Domain.Devices.ExecutionEndpoints.Projections;

public class ExecutionEndpointReadinessProjection : AuditedEntity
{
    private readonly List<ExecutionEndpointCapabilityProjection> _capabilities = [];
    public Guid KioskId { get; private set; }
    public Guid KioskExecutionEndpointId { get; private set; }
    public Guid SourceExecutorId { get; private set; }
    public long StateRevision { get; private set; }
    public ExecutionReadinessState Readiness { get; private set; }
    public ExecutionActivityState Activity { get; private set; }
    public ExecutionSafetyState Safety { get; private set; }
    public Guid? CurrentCommandId { get; private set; }
    public PhysicalOutputState PhysicalOutputState { get; private set; }
    public string? FaultCode { get; private set; }
    public DateTimeOffset ExecutorReportedAt { get; private set; }
    public DateTimeOffset CloudReceivedAt { get; private set; }
    public virtual Kiosk Kiosk { get; private set; } = null!;
    public virtual KioskExecutionEndpoint KioskExecutionEndpoint { get; private set; } = null!;
    public IReadOnlyCollection<ExecutionEndpointCapabilityProjection> Capabilities => _capabilities;

    private ExecutionEndpointReadinessProjection() { }

    public static ExecutionEndpointReadinessProjection Create(Guid kioskId, Guid endpointId, Guid executorId,
        long revision, ExecutionReadinessState readiness, ExecutionActivityState activity,
        ExecutionSafetyState safety, Guid? currentCommandId, PhysicalOutputState outputState,
        string? faultCode, DateTimeOffset reportedAt, DateTimeOffset receivedAt)
    {
        var item = new ExecutionEndpointReadinessProjection { KioskId = kioskId, KioskExecutionEndpointId = endpointId, SourceExecutorId = executorId };
        item.Apply(revision, readiness, activity, safety, currentCommandId, outputState, faultCode, reportedAt, receivedAt);
        return item;
    }

    public void Apply(long revision, ExecutionReadinessState readiness, ExecutionActivityState activity,
        ExecutionSafetyState safety, Guid? currentCommandId, PhysicalOutputState outputState,
        string? faultCode, DateTimeOffset reportedAt, DateTimeOffset receivedAt)
    {
        if (revision <= StateRevision) throw new DomainRuleException("Readiness revision must be newer than the current projection.");
        StateRevision = revision; Readiness = readiness; Activity = activity; Safety = safety;
        CurrentCommandId = currentCommandId; PhysicalOutputState = outputState;
        FaultCode = string.IsNullOrWhiteSpace(faultCode) ? null : faultCode.Trim();
        ExecutorReportedAt = reportedAt; CloudReceivedAt = receivedAt;
    }
}

public class ExecutionEndpointCapabilityProjection : GuidEntity
{
    public Guid ExecutionEndpointReadinessProjectionId { get; set; }
    public string CapabilityCode { get; set; } = null!;
    public string? WorkcellCode { get; set; }
    public bool IsAvailable { get; set; }
    public string? UnavailableReason { get; set; }
    public virtual ExecutionEndpointReadinessProjection ReadinessProjection { get; set; } = null!;
}
