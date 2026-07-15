using Application.Orders.Management.Results;
using Domain.ProductionExecution.Projections;
using Domain.Sync.Entities;

namespace Application.Orders.Management.Mapping;

internal static class ExecutionAttemptResultMapper
{
    public static ExecutionAttemptResult ToResult(
        EdgeCommand command,
        OrderExecutionRecord? executionRecord)
    {
        return new ExecutionAttemptResult
        {
            SourceCommandId = command.Id,
            OrderId = command.OrderId!.Value,
            DispatchAttemptNo = command.DispatchAttemptNo!.Value,
            KioskExecutionEndpointId = command.TargetExecutionEndpointId,
            CommandStatus = command.Status.ToString(),
            CreatedAt = command.CreatedAt,
            RequestedByAccountId = command.CreatedByAccountId,
            CommandExpiryAt = command.CommandExpiryAt,
            DeliveredAt = command.DeliveredAt,
            RespondedAt = command.RespondedAt,
            RejectionCode = command.RejectionCode,
            RejectionMessage = command.RejectionMessage,
            ExecutionProfile = executionRecord?.ExecutionProfile.ToString(),
            SourceConfigurationReleaseId = executionRecord?.SourceConfigurationReleaseId,
            ReleaseChecksum = executionRecord?.ReleaseChecksum,
            ExecutionStatus = executionRecord?.Status.ToString(),
            ObservationStatus = executionRecord?.ObservationStatus.ToString(),
            CustomerExecutionStatus = executionRecord?.CustomerExecutionStatus.ToString(),
            SourceExecutorId = executionRecord?.SourceExecutorId,
            LastAppliedSourceEventId = executionRecord?.LastAppliedSourceEventId,
            LastAppliedSequenceNumber = executionRecord?.LastAppliedSequenceNumber,
            LastEdgeCreatedAt = executionRecord?.LastEdgeCreatedAt,
            LastExecutorReportedAt = executionRecord?.LastExecutorReportedAt,
            CloudReceivedAt = executionRecord?.CloudReceivedAt
        };
    }

    public static ProductionExecutionResult ToResult(ProductionExecutionRecord record)
    {
        return new ProductionExecutionResult
        {
            Id = record.Id,
            SourceProductionJobId = record.SourceProductionJobId,
            OrderItemId = record.OrderItemId,
            ProductionUnitNo = record.ProductionUnitNo,
            ProductionUnitQuantity = record.ProductionUnitQuantity,
            WorkcellId = record.WorkcellId,
            ControllerId = record.ControllerId,
            ExecutionPlanChecksum = record.ExecutionPlanChecksum,
            ActiveSetVersion = record.ActiveSetVersion,
            ActiveSetChecksum = record.ActiveSetChecksum,
            Status = record.Status.ToString(),
            PhysicalOutputState = record.PhysicalOutputState.ToString(),
            ErrorCode = record.ErrorCode,
            ErrorMessage = record.ErrorMessage,
            SourceExecutorId = record.SourceExecutorId,
            LastAppliedSourceEventId = record.LastAppliedSourceEventId,
            LastAppliedSequenceNumber = record.LastAppliedSequenceNumber,
            LastEdgeCreatedAt = record.LastEdgeCreatedAt,
            LastExecutorReportedAt = record.LastExecutorReportedAt,
            CloudReceivedAt = record.CloudReceivedAt
        };
    }

    public static ExecutionAttemptReferenceResult ToReference(EdgeCommand command)
    {
        return new ExecutionAttemptReferenceResult
        {
            SourceCommandId = command.Id,
            DispatchAttemptNo = command.DispatchAttemptNo!.Value,
            CommandStatus = command.Status.ToString(),
            CreatedAt = command.CreatedAt
        };
    }

    public static ExecutionDeliveryAttemptResult ToResult(EdgeCommandDeliveryAttempt attempt)
    {
        return new ExecutionDeliveryAttemptResult
        {
            DeliveryAttemptNo = attempt.DeliveryAttemptNo,
            SentAt = attempt.SentAt,
            Outcome = attempt.Outcome.ToString(),
            ResponseCode = attempt.ResponseCode,
            ResponseMessage = attempt.ResponseMessage
        };
    }
}
