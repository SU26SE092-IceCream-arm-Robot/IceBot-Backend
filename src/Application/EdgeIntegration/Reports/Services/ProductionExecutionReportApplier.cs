using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.CommandDelivery.Commands;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.EdgeIntegration.Dispatch.Contracts;
using Application.EdgeIntegration.Dispatch.Services;
using Application.EdgeIntegration.Reports.Commands;
using Application.EdgeIntegration.Timeouts.Commands;
using Domain.Common;
using Domain.Devices.ExecutionEndpoints;
using Domain.ProductionExecution.Enums;
using Domain.ProductionExecution.Projections;
using Domain.Sync.Entities;
using Domain.Sync.Enums;

namespace Application.EdgeIntegration.Reports.Services;

internal static class ProductionExecutionReportApplier
{
    public static async Task<bool> ApplyAsync(
        IExecutionReportUnitOfWork unitOfWork,
        ExecutionReportProcessingContext context,
        CancellationToken cancellationToken)
    {
        var command = context.Command;
        var endpoint = context.Endpoint;
        var edgeCommand = context.EdgeCommand;
        if (edgeCommand.CommandType != EdgeCommandType.ExecuteOrder)
            throw new DomainRuleException("Production execution reports require an execute-order command.");

        ExecuteOrderReleaseValidator.Validate(command, edgeCommand);
        if (command.SourceProductionJobId.HasValue)
            ValidateProductionJobAgainstCommand(command, edgeCommand.PayloadJson);
        var status = ExecutionReportStatusMapper.ParseProductionStatus(command.Status);
        var physicalOutputState = ExecutionReportStatusMapper.ToPhysicalOutputState(command.PhysicalOutputMayHaveOccurred);
        var productionApplied = await ApplyProductionRecordAsync(
            unitOfWork, context, status, physicalOutputState, cancellationToken);
        var orderApplied = !command.SourceProductionJobId.HasValue && await ApplyOrderRecordAsync(
            unitOfWork, context, status, cancellationToken);

        if (orderApplied)
            await OrderExecutionLifecycleApplier.ApplyAsync(
                unitOfWork, context, status, cancellationToken);
        var stockApplied = command.StockMovements.Count > 0 &&
            await ExecutionStockEvidenceApplier.ApplyAsync(
                unitOfWork, context, cancellationToken);
        return productionApplied || orderApplied || stockApplied;
    }

    private static async Task<bool> ApplyProductionRecordAsync(
        IProductionExecutionReportStore store,
        ExecutionReportProcessingContext context,
        ProductionExecutionStatus status,
        PhysicalOutputState physicalOutputState,
        CancellationToken cancellationToken)
    {
        var command = context.Command;
        var endpoint = context.Endpoint;
        var edgeCommand = context.EdgeCommand;
        if (!command.SourceProductionJobId.HasValue) return false;
        var record = await store.GetProductionExecutionRecordAsync(
            edgeCommand.Id, command.SourceProductionJobId.Value, cancellationToken);
        if (record is null)
        {
            record = ProductionExecutionRecord.Create(
                edgeCommand.Id, endpoint.Id, endpoint.ExecutionProfile, context.SourceExecutorId, command.SourceEventId,
                command.SequenceNumber, command.EdgeCreatedAt, context.ExecutorReportedAt, context.CloudReceivedAt, status,
                physicalOutputState, command.SourceProductionJobId.Value,
                command.OrderItemId!.Value, command.ProductionUnitNo!.Value, command.ProductionUnitQuantity!.Value,
                command.WorkcellId, command.ControllerId,
                command.ExecutionPlanChecksum, command.ActiveSetVersion, command.ActiveSetChecksum,
                command.ErrorCode, command.ErrorMessage);
            await store.AddProductionExecutionRecordAsync(record, cancellationToken);
            return true;
        }

        record.EnsureSameProvenance(
            command.OrderItemId!.Value,
            command.ProductionUnitNo!.Value,
            command.ProductionUnitQuantity!.Value,
            command.WorkcellId,
            command.ControllerId,
            command.ExecutionPlanChecksum,
            command.ActiveSetVersion,
            command.ActiveSetChecksum);

        return record.ApplyObservation(
            command.SourceEventId, command.SequenceNumber, command.EdgeCreatedAt, context.ExecutorReportedAt,
            context.CloudReceivedAt, status, physicalOutputState, command.ErrorCode, command.ErrorMessage);
    }

    private static void ValidateProductionJobAgainstCommand(
        IngestExecutionReportCommand command,
        string payloadJson)
    {
        var payload = ExecuteOrderCommandPayloadCodec.DeserializeAndValidateFull(payloadJson);
        var line = payload.OrderLines.SingleOrDefault(candidate => candidate.OrderItemId == command.OrderItemId)
            ?? throw new DomainRuleException("Production job order item is not present in the dispatched command.");
        if ((long)command.ProductionUnitNo!.Value + command.ProductionUnitQuantity!.Value - 1 > line.Quantity)
            throw new DomainRuleException("Production job unit range exceeds the dispatched order-line quantity.");
    }

    private static async Task<bool> ApplyOrderRecordAsync(
        IProductionExecutionReportStore store,
        ExecutionReportProcessingContext context,
        ProductionExecutionStatus status,
        CancellationToken cancellationToken)
    {
        var command = context.Command;
        var endpoint = context.Endpoint;
        var edgeCommand = context.EdgeCommand;
        if (edgeCommand.OrderId is null || edgeCommand.DispatchAttemptNo is null) return false;
        if (command.SourceConfigurationReleaseId is null || command.SourceConfigurationReleaseId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.ReleaseChecksum))
            throw new DomainRuleException("Order execution reports require source configuration release and checksum.");

        var customerStatus = ExecutionReportStatusMapper.ToCustomerStatus(status, command.PhysicalOutputMayHaveOccurred);
        var record = await store.GetOrderExecutionRecordAsync(edgeCommand.Id, cancellationToken);
        if (record is null)
        {
            record = OrderExecutionRecord.Create(
                edgeCommand.OrderId.Value, edgeCommand.Id, edgeCommand.DispatchAttemptNo.Value, endpoint.Id,
                endpoint.ExecutionProfile, context.SourceExecutorId, command.SourceConfigurationReleaseId.Value,
                command.ReleaseChecksum, command.SourceEventId, command.SequenceNumber, command.EdgeCreatedAt,
                context.ExecutorReportedAt, context.CloudReceivedAt, status, ExecutionObservationStatus.Fresh, customerStatus);
            await store.AddOrderExecutionRecordAsync(record, cancellationToken);
            return true;
        }

        if (record.SourceConfigurationReleaseId != command.SourceConfigurationReleaseId.Value ||
            !string.Equals(record.ReleaseChecksum, command.ReleaseChecksum, StringComparison.Ordinal))
            throw new DomainRuleException("Order execution report release does not match the dispatched command.");

        return record.ApplyObservation(
            command.SourceEventId, command.SequenceNumber, command.EdgeCreatedAt, context.ExecutorReportedAt,
            context.CloudReceivedAt, status, ExecutionObservationStatus.Fresh, customerStatus);
    }
}
