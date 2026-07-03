using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Commands;
using Domain.Common;
using Domain.Devices.ExecutionEndpoints;
using Domain.ProductionExecution.Enums;
using Domain.ProductionExecution.Projections;
using Domain.Sync.Entities;
using Domain.Sync.Enums;

namespace Application.EdgeIntegration.Services;

internal static class ProductionExecutionReportApplier
{
    public static async Task<bool> ApplyAsync(
        IProductionExecutionReportStore productionStore,
        IExecutionStockEvidenceStore stockStore,
        IngestExecutionReportCommand command,
        KioskExecutionEndpoint endpoint,
        Guid sourceExecutorId,
        EdgeCommand edgeCommand,
        DateTimeOffset executorReportedAt,
        DateTimeOffset cloudReceivedAt,
        ExecutionReportNotifications notifications,
        CancellationToken cancellationToken)
    {
        if (edgeCommand.CommandType != EdgeCommandType.ExecuteOrder)
            throw new DomainRuleException("Production execution reports require an execute-order command.");

        ExecutionReportRules.ValidateRelease(command, edgeCommand);
        var status = ExecutionReportRules.ParseProductionStatus(command.Status);
        var physicalOutputState = ExecutionReportRules.ToPhysicalOutputState(command.PhysicalOutputMayHaveOccurred);
        var productionApplied = await ApplyProductionRecordAsync(
            productionStore, command, endpoint, sourceExecutorId, edgeCommand, executorReportedAt,
            cloudReceivedAt, status, physicalOutputState, cancellationToken);
        var orderApplied = !command.SourceProductionJobId.HasValue && await ApplyOrderRecordAsync(
            productionStore, command, endpoint, sourceExecutorId, edgeCommand, executorReportedAt,
            cloudReceivedAt, status, cancellationToken);

        if (orderApplied)
            await OrderExecutionLifecycleApplier.ApplyAsync(
                productionStore, command, edgeCommand, status, executorReportedAt, notifications, cancellationToken);
        if (productionApplied && command.StockMovements.Count > 0)
            await ExecutionStockEvidenceApplier.ApplyAsync(
                stockStore, command, endpoint, sourceExecutorId, edgeCommand, notifications, cancellationToken);
        return productionApplied || orderApplied;
    }

    private static async Task<bool> ApplyProductionRecordAsync(
        IProductionExecutionReportStore store,
        IngestExecutionReportCommand command,
        KioskExecutionEndpoint endpoint,
        Guid sourceExecutorId,
        EdgeCommand edgeCommand,
        DateTimeOffset executorReportedAt,
        DateTimeOffset cloudReceivedAt,
        ProductionExecutionStatus status,
        PhysicalOutputState physicalOutputState,
        CancellationToken cancellationToken)
    {
        if (!command.SourceProductionJobId.HasValue) return false;
        var record = await store.GetProductionExecutionRecordAsync(
            edgeCommand.Id, command.SourceProductionJobId.Value, cancellationToken);
        if (record is null)
        {
            record = ProductionExecutionRecord.Create(
                edgeCommand.Id, endpoint.Id, endpoint.ExecutionProfile, sourceExecutorId, command.SourceEventId,
                command.SequenceNumber, command.EdgeCreatedAt, executorReportedAt, cloudReceivedAt, status,
                physicalOutputState, command.SourceProductionJobId.Value, command.WorkcellId, command.ControllerId,
                command.ExecutionPlanChecksum, command.ActiveSetVersion, command.ActiveSetChecksum,
                command.ErrorCode, command.ErrorMessage);
            await store.AddProductionExecutionRecordAsync(record, cancellationToken);
            return true;
        }

        return record.ApplyObservation(
            command.SourceEventId, command.SequenceNumber, command.EdgeCreatedAt, executorReportedAt,
            cloudReceivedAt, status, physicalOutputState, command.ErrorCode, command.ErrorMessage);
    }

    private static async Task<bool> ApplyOrderRecordAsync(
        IProductionExecutionReportStore store,
        IngestExecutionReportCommand command,
        KioskExecutionEndpoint endpoint,
        Guid sourceExecutorId,
        EdgeCommand edgeCommand,
        DateTimeOffset executorReportedAt,
        DateTimeOffset cloudReceivedAt,
        ProductionExecutionStatus status,
        CancellationToken cancellationToken)
    {
        if (edgeCommand.OrderId is null || edgeCommand.DispatchAttemptNo is null) return false;
        if (command.SourceConfigurationReleaseId is null || command.SourceConfigurationReleaseId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.ReleaseChecksum))
            throw new DomainRuleException("Order execution reports require source configuration release and checksum.");

        var customerStatus = ExecutionReportRules.MapCustomerStatus(status, command.PhysicalOutputMayHaveOccurred);
        var record = await store.GetOrderExecutionRecordAsync(edgeCommand.Id, cancellationToken);
        if (record is null)
        {
            record = OrderExecutionRecord.Create(
                edgeCommand.OrderId.Value, edgeCommand.Id, edgeCommand.DispatchAttemptNo.Value, endpoint.Id,
                endpoint.ExecutionProfile, sourceExecutorId, command.SourceConfigurationReleaseId.Value,
                command.ReleaseChecksum, command.SourceEventId, command.SequenceNumber, command.EdgeCreatedAt,
                executorReportedAt, cloudReceivedAt, status, ExecutionObservationStatus.Fresh, customerStatus);
            await store.AddOrderExecutionRecordAsync(record, cancellationToken);
            return true;
        }

        if (record.SourceConfigurationReleaseId != command.SourceConfigurationReleaseId.Value ||
            !string.Equals(record.ReleaseChecksum, command.ReleaseChecksum, StringComparison.Ordinal))
            throw new DomainRuleException("Order execution report release does not match the dispatched command.");

        return record.ApplyObservation(
            command.SourceEventId, command.SequenceNumber, command.EdgeCreatedAt, executorReportedAt,
            cloudReceivedAt, status, ExecutionObservationStatus.Fresh, customerStatus);
    }
}
