using Domain.Common;
using Domain.Devices.Enums;
using Domain.ProductionExecution.Enums;

namespace Domain.ProductionExecution.Projections;

public class OrderExecutionRecord : AuditedEntity
{
    public Guid OrderId { get; private set; }
    public Guid SourceCommandId { get; private set; }
    public int DispatchAttemptNo { get; private set; }
    public Guid KioskExecutionEndpointId { get; private set; }
    public KioskExecutionProfile ExecutionProfile { get; private set; }
    public Guid SourceConfigurationReleaseId { get; private set; }
    public string ReleaseChecksum { get; private set; } = null!;
    public ProductionExecutionStatus Status { get; private set; }
    public ExecutionObservationStatus ObservationStatus { get; private set; } = ExecutionObservationStatus.Fresh;
    public CustomerExecutionStatus CustomerExecutionStatus { get; private set; }
    public Guid SourceExecutorId { get; private set; }
    public Guid LastAppliedSourceEventId { get; private set; }
    public long LastAppliedSequenceNumber { get; private set; }
    public DateTimeOffset LastEdgeCreatedAt { get; private set; }
    public DateTimeOffset LastExecutorReportedAt { get; private set; }
    public DateTimeOffset CloudReceivedAt { get; private set; }

    public virtual Domain.Devices.Entities.KioskExecutionEndpoint KioskExecutionEndpoint { get; private set; } = null!;

    public virtual Domain.Sync.Entities.EdgeCommand SourceCommand { get; private set; } = null!;

    public virtual Domain.ProductionConfiguration.Entities.ConfigurationRelease SourceConfigurationRelease { get; private set; } = null!;

    private OrderExecutionRecord()
    {
    }

    public static OrderExecutionRecord Create(
        Guid orderId,
        Guid sourceCommandId,
        int dispatchAttemptNo,
        Guid kioskExecutionEndpointId,
        KioskExecutionProfile executionProfile,
        Guid sourceExecutorId,
        Guid sourceConfigurationReleaseId,
        string releaseChecksum,
        Guid sourceEventId,
        long sequenceNumber,
        DateTimeOffset edgeCreatedAt,
        DateTimeOffset executorReportedAt,
        DateTimeOffset cloudReceivedAt,
        ProductionExecutionStatus status,
        ExecutionObservationStatus observationStatus,
        CustomerExecutionStatus customerExecutionStatus)
    {
        if (orderId == Guid.Empty || sourceCommandId == Guid.Empty || kioskExecutionEndpointId == Guid.Empty ||
            sourceConfigurationReleaseId == Guid.Empty || sourceExecutorId == Guid.Empty || sourceEventId == Guid.Empty || dispatchAttemptNo <= 0 ||
            sequenceNumber <= 0 || string.IsNullOrWhiteSpace(releaseChecksum))
        {
            throw new DomainRuleException("Order execution provenance, sequence, and release checksum are required.");
        }

        return new OrderExecutionRecord
        {
            OrderId = orderId,
            SourceCommandId = sourceCommandId,
            DispatchAttemptNo = dispatchAttemptNo,
            KioskExecutionEndpointId = kioskExecutionEndpointId,
            ExecutionProfile = executionProfile,
            SourceExecutorId = sourceExecutorId,
            SourceConfigurationReleaseId = sourceConfigurationReleaseId,
            ReleaseChecksum = releaseChecksum.Trim(),
            Status = status,
            ObservationStatus = observationStatus,
            CustomerExecutionStatus = customerExecutionStatus,
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
        ExecutionObservationStatus observationStatus,
        CustomerExecutionStatus customerExecutionStatus)
    {
        if (!CanApply(sourceEventId, sequenceNumber))
        {
            return false;
        }

        EnsureStatusTransition(Status, status);
        Status = status;
        ObservationStatus = observationStatus;
        CustomerExecutionStatus = customerExecutionStatus;
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
            throw new DomainRuleException("A sequence number cannot identify different order execution events.");
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
            ProductionExecutionStatus.Accepted => next is ProductionExecutionStatus.Running or ProductionExecutionStatus.Failed or ProductionExecutionStatus.RequiresManualIntervention,
            ProductionExecutionStatus.Running => next is ProductionExecutionStatus.Completed or ProductionExecutionStatus.Failed or ProductionExecutionStatus.RequiresManualIntervention,
            _ => false
        };

        if (!valid)
        {
            throw new DomainRuleException("Order execution status transition is not allowed.");
        }
    }
}
