using Domain.Devices.ExecutionEndpoints;
using Domain.Common;
using Domain.Devices.Catalog;
using Domain.ProductionExecution.Enums;

namespace Domain.ProductionExecution.Projections;

public class ProductionExecutionRecord : AuditedEntity
{
    public Guid SourceCommandId { get; private set; }
    public Guid KioskExecutionEndpointId { get; private set; }
    public KioskExecutionProfile ExecutionProfile { get; private set; }
    public Guid SourceProductionJobId { get; private set; }
    public Guid? WorkcellId { get; private set; }
    public Guid? ControllerId { get; private set; }
    public string? ExecutionPlanChecksum { get; private set; }
    public long? ActiveSetVersion { get; private set; }
    public string? ActiveSetChecksum { get; private set; }
    public ProductionExecutionStatus Status { get; private set; }
    public PhysicalOutputState PhysicalOutputState { get; private set; } = PhysicalOutputState.Unknown;
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Guid SourceExecutorId { get; private set; }
    public Guid LastAppliedSourceEventId { get; private set; }
    public long LastAppliedSequenceNumber { get; private set; }
    public DateTimeOffset LastEdgeCreatedAt { get; private set; }
    public DateTimeOffset LastExecutorReportedAt { get; private set; }
    public DateTimeOffset CloudReceivedAt { get; private set; }

    public virtual Domain.Devices.ExecutionEndpoints.KioskExecutionEndpoint KioskExecutionEndpoint { get; private set; } = null!;

    public virtual Domain.Sync.Entities.EdgeCommand SourceCommand { get; private set; } = null!;

    private ProductionExecutionRecord()
    {
    }

    public static ProductionExecutionRecord Create(
        Guid sourceCommandId,
        Guid kioskExecutionEndpointId,
        KioskExecutionProfile executionProfile,
        Guid sourceExecutorId,
        Guid sourceEventId,
        long sequenceNumber,
        DateTimeOffset edgeCreatedAt,
        DateTimeOffset executorReportedAt,
        DateTimeOffset cloudReceivedAt,
        ProductionExecutionStatus status,
        PhysicalOutputState physicalOutputState,
        Guid sourceProductionJobId,
        Guid? workcellId = null,
        Guid? controllerId = null,
        string? executionPlanChecksum = null,
        long? activeSetVersion = null,
        string? activeSetChecksum = null,
        string? errorCode = null,
        string? errorMessage = null)
    {
        if (sourceCommandId == Guid.Empty || sourceProductionJobId == Guid.Empty ||
            kioskExecutionEndpointId == Guid.Empty || sourceExecutorId == Guid.Empty ||
            sourceEventId == Guid.Empty || sequenceNumber <= 0)
        {
            throw new DomainRuleException("Production execution provenance and sequence are required.");
        }

        return new ProductionExecutionRecord
        {
            SourceCommandId = sourceCommandId,
            KioskExecutionEndpointId = kioskExecutionEndpointId,
            ExecutionProfile = executionProfile,
            SourceExecutorId = sourceExecutorId,
            SourceProductionJobId = sourceProductionJobId,
            WorkcellId = workcellId,
            ControllerId = controllerId,
            ExecutionPlanChecksum = executionPlanChecksum,
            ActiveSetVersion = activeSetVersion,
            ActiveSetChecksum = activeSetChecksum,
            Status = status,
            PhysicalOutputState = physicalOutputState,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            LastAppliedSourceEventId = sourceEventId,
            LastAppliedSequenceNumber = sequenceNumber,
            LastEdgeCreatedAt = edgeCreatedAt,
            LastExecutorReportedAt = executorReportedAt,
            CloudReceivedAt = cloudReceivedAt
        };
    }

    public bool ApplyObservation(
        Guid sourceEventId,
        long sequenceNumber,
        DateTimeOffset edgeCreatedAt,
        DateTimeOffset executorReportedAt,
        DateTimeOffset cloudReceivedAt,
        ProductionExecutionStatus status,
        PhysicalOutputState physicalOutputState,
        string? errorCode = null,
        string? errorMessage = null)
    {
        if (!CanApply(sourceEventId, sequenceNumber))
        {
            return false;
        }

        EnsureStatusTransition(Status, status);
        EnsurePhysicalOutputTransition(PhysicalOutputState, physicalOutputState);
        Status = status;
        PhysicalOutputState = physicalOutputState;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        LastAppliedSourceEventId = sourceEventId;
        LastAppliedSequenceNumber = sequenceNumber;
        LastEdgeCreatedAt = edgeCreatedAt;
        LastExecutorReportedAt = executorReportedAt;
        CloudReceivedAt = cloudReceivedAt;
        return true;
    }

    private bool CanApply(Guid sourceEventId, long sequenceNumber)
    {
        if (sourceEventId == Guid.Empty || sequenceNumber <= 0)
        {
            throw new DomainRuleException("Execution event id and sequence number are required.");
        }

        if (sequenceNumber < LastAppliedSequenceNumber ||
            (sequenceNumber == LastAppliedSequenceNumber && sourceEventId == LastAppliedSourceEventId))
        {
            return false;
        }

        if (sequenceNumber == LastAppliedSequenceNumber)
        {
            throw new DomainRuleException("A sequence number cannot identify different production execution events.");
        }

        return true;
    }

    private static void EnsureStatusTransition(ProductionExecutionStatus current, ProductionExecutionStatus next)
    {
        if (current == next)
        {
            return;
        }

        var valid = current switch
        {
            ProductionExecutionStatus.Accepted => next is ProductionExecutionStatus.Running or ProductionExecutionStatus.Completed or ProductionExecutionStatus.Failed or ProductionExecutionStatus.RequiresManualIntervention,
            ProductionExecutionStatus.Running => next is ProductionExecutionStatus.Completed or ProductionExecutionStatus.Failed or ProductionExecutionStatus.RequiresManualIntervention,
            _ => false
        };

        if (!valid)
        {
            throw new DomainRuleException("Production execution status transition is not allowed.");
        }
    }

    private static void EnsurePhysicalOutputTransition(PhysicalOutputState current, PhysicalOutputState next)
    {
        if (current == next || current == PhysicalOutputState.Unknown ||
            (current == PhysicalOutputState.No && next == PhysicalOutputState.Yes))
        {
            return;
        }

        throw new DomainRuleException("Physical output state cannot move from yes back to no or unknown.");
    }
}
