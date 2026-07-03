using Domain.Sync.Ingestion;
using Domain.Devices.ExecutionEndpoints;
using System.Text.Json;
using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Contracts;
using Application.EdgeIntegration.Results;
using Application.EdgeIntegration.Services;
using Application.Shared.Utils;
using Application.Shared.Wrappers;
using Domain.Common;
using Domain.Devices.Entities;
using Domain.Devices.Enums;
using Domain.Inventory.Entities;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.ProductionExecution.Enums;
using Domain.ProductionExecution.Projections;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using Microsoft.Extensions.Options;
using Application.EdgeIntegration.Observability;

namespace Application.EdgeIntegration.Commands;

public sealed class IngestExecutionReportCommandHandler
{
    private readonly IExecutionReportReceiptStore _receiptStore;
    private readonly IDeploymentReportStore _deploymentStore;
    private readonly IProductionExecutionReportStore _productionStore;
    private readonly IExecutionStockEvidenceStore _stockStore;
    private readonly IRealtimeNotificationPublisher _publisher;
    private readonly ExecutionReportIngestionOptions _options;

    public IngestExecutionReportCommandHandler(
        IExecutionReportReceiptStore receiptStore,
        IDeploymentReportStore deploymentStore,
        IProductionExecutionReportStore productionStore,
        IExecutionStockEvidenceStore stockStore,
        IRealtimeNotificationPublisher publisher,
        IOptions<ExecutionReportIngestionOptions> options)
    {
        _receiptStore = receiptStore;
        _deploymentStore = deploymentStore;
        _productionStore = productionStore;
        _stockStore = stockStore;
        _publisher = publisher;
        _options = options.Value;
    }

    public async Task<ApiResult<ExecutionReportIngestResult>> HandleAsync(
        IngestExecutionReportCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationError = ExecutionReportRules.Validate(command, DateTimeOffset.UtcNow, _options.MaxFutureClockSkewSeconds);
        if (validationError is not null)
        {
            return ApiResult<ExecutionReportIngestResult>.Fail(validationError, 400);
        }

        var endpoint = await _receiptStore.GetEndpointForReportAuthAsync(command.EndpointId, cancellationToken);
        if (!ExecutionReportRules.IsUsableEndpoint(endpoint, command))
        {
            return ApiResult<ExecutionReportIngestResult>.Fail("Execution endpoint authentication failed.", 401);
        }

        var sourceExecutorId = ExecutionReportRules.GetSourceExecutorId(endpoint!);
        if (sourceExecutorId is null)
        {
            return ApiResult<ExecutionReportIngestResult>.Fail("Execution endpoint profile identity is missing.", 400);
        }

        var notifications = new ExecutionReportNotifications();
        var result = await _receiptStore.ExecuteReportIngestionAsync(
            sourceExecutorId.Value,
            command.SourceEventId,
            command.CommandId,
            ct => IngestLockedAsync(command, endpoint!, sourceExecutorId.Value, notifications, ct),
            cancellationToken);

        if (result.Succeeded && !result.Data!.Duplicate)
        {
            var executorReportedAt = command.ExecutorReportedAt ?? command.EdgeCreatedAt;
            IceBotEdgeMetrics.RecordExecutionReportLag(
                DateTimeOffset.UtcNow - executorReportedAt,
                command.ReportType);
            await PublishNotificationsAsync(notifications, cancellationToken);
        }

        return result;
    }

    private async Task<ApiResult<ExecutionReportIngestResult>> IngestLockedAsync(
        IngestExecutionReportCommand command,
        KioskExecutionEndpoint endpoint,
        Guid sourceExecutorId,
        ExecutionReportNotifications notifications,
        CancellationToken cancellationToken)
    {
        var cloudReceivedAt = DateTimeOffset.UtcNow;
        var candidateInboxEvent = ExecutionReportRules.BuildInboxEvent(command, sourceExecutorId, cloudReceivedAt);
        var existingEvent = await _receiptStore.GetSyncEventByEventIdAsync(
            sourceExecutorId, command.SourceEventId, cancellationToken);
        if (existingEvent?.Status is SyncEventStatus.Processed or SyncEventStatus.Ignored)
        {
            if (!ExecutionReportRules.MatchesIdentity(existingEvent, candidateInboxEvent))
            {
                return ApiResult<ExecutionReportIngestResult>.Fail(
                    "Execution report source event id was reused with different command or payload.", 409);
            }

            return ApiResult<ExecutionReportIngestResult>.Success(
                ExecutionReportRules.BuildResult(command, applied: false, duplicate: true),
                "Execution report already ingested.");
        }

        if (command.StockMovements.Count > 0)
        {
            await _stockStore.AcquireStockMovementLocksAsync(
                command.StockMovements.Select(item => item.SourceEventId),
                cancellationToken);
        }

        var edgeCommand = await _receiptStore.GetCommandAsync(command.CommandId, cancellationToken);
        if (edgeCommand is null ||
            edgeCommand.KioskId != command.KioskId ||
            edgeCommand.TargetExecutionEndpointId != command.EndpointId ||
            edgeCommand.Status != EdgeCommandStatus.Accepted)
        {
            return ApiResult<ExecutionReportIngestResult>.Fail("Accepted edge command not found for execution report.", 404);
        }

        var executorReportedAt = command.ExecutorReportedAt ?? command.EdgeCreatedAt;
        var inboxEvent = existingEvent ?? candidateInboxEvent;

        try
        {
            var applied = await ApplyReportAsync(
                command,
                endpoint,
                sourceExecutorId,
                edgeCommand,
                executorReportedAt,
                cloudReceivedAt,
                notifications,
                cancellationToken);

            inboxEvent.MarkProcessed(cloudReceivedAt);
            if (existingEvent is null)
                await _receiptStore.AddSyncEventAsync(inboxEvent, cancellationToken);
            await _receiptStore.SaveChangesAsync(cancellationToken);

            return ApiResult<ExecutionReportIngestResult>.Success(
                ExecutionReportRules.BuildResult(command, applied, duplicate: false),
                applied ? "Execution report applied successfully." : "Execution report accepted but did not change projection.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<ExecutionReportIngestResult>.Fail(ex.Message, 400);
        }
    }

    private async Task<bool> ApplyReportAsync(
        IngestExecutionReportCommand command,
        KioskExecutionEndpoint endpoint,
        Guid sourceExecutorId,
        EdgeCommand edgeCommand,
        DateTimeOffset executorReportedAt,
        DateTimeOffset cloudReceivedAt,
        ExecutionReportNotifications notifications,
        CancellationToken cancellationToken)
    {
        if (string.Equals(command.ReportType, "Deployment", StringComparison.OrdinalIgnoreCase))
        {
            return await DeploymentExecutionReportApplier.ApplyAsync(
                _deploymentStore, command, endpoint, cloudReceivedAt, cancellationToken);
        }

        if (string.Equals(command.ReportType, "ProductionExecution", StringComparison.OrdinalIgnoreCase))
        {
            return await ProductionExecutionReportApplier.ApplyAsync(
                _productionStore, _stockStore, command, endpoint, sourceExecutorId, edgeCommand,
                executorReportedAt, cloudReceivedAt, notifications, cancellationToken);
        }

        throw new DomainRuleException("Unsupported execution report type.");
    }

    private async Task PublishNotificationsAsync(
        ExecutionReportNotifications notifications,
        CancellationToken cancellationToken)
    {
        if (notifications.OrderStatusChanged is not null)
        {
            await _publisher.PublishOrderStatusChangedAsync(
                notifications.OrderStatusChanged,
                cancellationToken);
        }

        foreach (var inventoryEvent in notifications.InventoryChanged)
        {
            await _publisher.PublishInventoryChangedAsync(inventoryEvent, cancellationToken);
        }
    }

}
