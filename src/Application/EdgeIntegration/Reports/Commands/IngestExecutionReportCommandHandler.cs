using Domain.Sync.Ingestion;
using Domain.Devices.ExecutionEndpoints;
using System.Text.Json;
using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Dispatch.Contracts;
using Application.EdgeIntegration.Reports.Contracts;
using Application.EdgeIntegration.CommandDelivery.Results;
using Application.EdgeIntegration.Dispatch.Results;
using Application.EdgeIntegration.Reports.Results;
using Application.EdgeIntegration.CommandDelivery.Services;
using Application.EdgeIntegration.Dispatch.Services;
using Application.EdgeIntegration.Reports.Services;
using Application.Orders.Support;
using Application.Shared.Wrappers;
using Domain.Common;
using Domain.Devices.Catalog;
using Domain.Inventory.Entities;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.ProductionExecution.Enums;
using Domain.ProductionExecution.Projections;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using Microsoft.Extensions.Options;
using Application.EdgeIntegration.Observability;

namespace Application.EdgeIntegration.Reports.Commands;

public sealed class IngestExecutionReportCommandHandler
{
    private readonly IExecutionReportUnitOfWork _unitOfWork;
    private readonly IRealtimeNotificationPublisher _publisher;
    private readonly ExecutionReportIngestionOptions _options;

    public IngestExecutionReportCommandHandler(
        IExecutionReportUnitOfWork unitOfWork,
        IRealtimeNotificationPublisher publisher,
        IOptions<ExecutionReportIngestionOptions> options)
    {
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _options = options.Value;
    }

    public async Task<ApiResult<ExecutionReportIngestResult>> HandleAsync(
        IngestExecutionReportCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationError = ExecutionReportValidator.Validate(command, DateTimeOffset.UtcNow, _options.MaxFutureClockSkewSeconds);
        if (validationError is not null)
        {
            return ApiResult<ExecutionReportIngestResult>.Fail(validationError, 400);
        }

        var endpoint = await _unitOfWork.GetEndpointForReportAuthAsync(command.EndpointId, cancellationToken);
        if (!ExecutionEndpointReportAuthenticator.IsUsable(endpoint, command))
        {
            return ApiResult<ExecutionReportIngestResult>.Fail("Execution endpoint authentication failed.", 401);
        }

        var sourceExecutorId = ExecutionEndpointReportAuthenticator.GetSourceExecutorId(endpoint!);
        if (sourceExecutorId is null)
        {
            return ApiResult<ExecutionReportIngestResult>.Fail("Execution endpoint profile identity is missing.", 400);
        }

        var notifications = new ExecutionReportNotifications();
        var result = await _unitOfWork.ExecuteReportIngestionAsync(
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
        var candidateInboxEvent = ExecutionReportInboxFactory.Create(command, sourceExecutorId, cloudReceivedAt);
        var existingEvent = await _unitOfWork.GetSyncEventByEventIdAsync(
            sourceExecutorId, command.SourceEventId, cancellationToken);
        if (existingEvent?.Status is SyncEventStatus.Processed or SyncEventStatus.Ignored)
        {
            if (!ExecutionReportInboxFactory.HasSameIdentity(existingEvent, candidateInboxEvent))
            {
                return ApiResult<ExecutionReportIngestResult>.Fail(
                    "Execution report source event id was reused with different command or payload.", 409);
            }

            return ApiResult<ExecutionReportIngestResult>.Success(
                BuildResult(command, applied: false, duplicate: true),
                "Execution report already ingested.");
        }

        if (command.StockMovements.Count > 0)
        {
            await _unitOfWork.AcquireStockMovementLocksAsync(
                command.StockMovements.Select(item => item.SourceEventId),
                cancellationToken);
        }

        var edgeCommand = await _unitOfWork.GetCommandAsync(command.CommandId, cancellationToken);
        if (edgeCommand is null ||
            edgeCommand.KioskId != command.KioskId ||
            edgeCommand.TargetExecutionEndpointId != command.EndpointId ||
            edgeCommand.Status != EdgeCommandStatus.Accepted)
        {
            return ApiResult<ExecutionReportIngestResult>.Fail("Accepted edge command not found for execution report.", 404);
        }

        var executorReportedAt = command.ExecutorReportedAt ?? command.EdgeCreatedAt;
        var inboxEvent = existingEvent ?? candidateInboxEvent;
        var processingContext = new ExecutionReportProcessingContext(
            command, endpoint, sourceExecutorId, edgeCommand, executorReportedAt, cloudReceivedAt, notifications);

        try
        {
            var applied = await ApplyReportAsync(processingContext, cancellationToken);

            inboxEvent.MarkProcessed(cloudReceivedAt);
            if (existingEvent is null)
                await _unitOfWork.AddSyncEventAsync(inboxEvent, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResult<ExecutionReportIngestResult>.Success(
                BuildResult(command, applied, duplicate: false),
                applied ? "Execution report applied successfully." : "Execution report accepted but did not change projection.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<ExecutionReportIngestResult>.Fail(ex.Message, 400);
        }
    }

    private async Task<bool> ApplyReportAsync(
        ExecutionReportProcessingContext context,
        CancellationToken cancellationToken)
    {
        var command = context.Command;
        if (string.Equals(command.ReportType, "Deployment", StringComparison.OrdinalIgnoreCase))
        {
            return await DeploymentExecutionReportApplier.ApplyAsync(
                _unitOfWork, context, cancellationToken);
        }

        if (string.Equals(command.ReportType, "ProductionExecution", StringComparison.OrdinalIgnoreCase))
        {
            return await ProductionExecutionReportApplier.ApplyAsync(
                _unitOfWork, context, cancellationToken);
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

    private static ExecutionReportIngestResult BuildResult(
        IngestExecutionReportCommand command,
        bool applied,
        bool duplicate) => new()
    {
        CommandId = command.CommandId,
        SourceEventId = command.SourceEventId,
        ReportType = command.ReportType.Trim(),
        Status = command.Status.Trim(),
        Applied = applied,
        Duplicate = duplicate
    };
}
