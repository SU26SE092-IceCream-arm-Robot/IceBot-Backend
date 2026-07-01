using Domain.Sync.Ingestion;
using Domain.Devices.ExecutionEndpoints;
using System.Text.Json;
using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Results;
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
        var validationError = Validate(command, DateTimeOffset.UtcNow, _options.MaxFutureClockSkewSeconds);
        if (validationError is not null)
        {
            return ApiResult<ExecutionReportIngestResult>.Fail(validationError, 400);
        }

        var endpoint = await _receiptStore.GetEndpointForReportAuthAsync(command.EndpointId, cancellationToken);
        if (!IsUsableEndpoint(endpoint, command))
        {
            return ApiResult<ExecutionReportIngestResult>.Fail("Execution endpoint authentication failed.", 401);
        }

        var sourceExecutorId = GetSourceExecutorId(endpoint!);
        if (sourceExecutorId is null)
        {
            return ApiResult<ExecutionReportIngestResult>.Fail("Execution endpoint profile identity is missing.", 400);
        }

        var notifications = new ReportNotifications();
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
        ReportNotifications notifications,
        CancellationToken cancellationToken)
    {
        var cloudReceivedAt = DateTimeOffset.UtcNow;
        var candidateInboxEvent = BuildInboxEvent(command, sourceExecutorId, cloudReceivedAt);
        var existingEvent = await _receiptStore.GetSyncEventByEventIdAsync(
            sourceExecutorId, command.SourceEventId, cancellationToken);
        if (existingEvent?.Status is SyncEventStatus.Processed or SyncEventStatus.Ignored)
        {
            if (!MatchesReportIdentity(existingEvent, candidateInboxEvent))
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
                BuildResult(command, applied, duplicate: false),
                applied ? "Execution report applied successfully." : "Execution report accepted but did not change projection.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<ExecutionReportIngestResult>.Fail(ex.Message, 400);
        }
    }

    private static string? Validate(
        IngestExecutionReportCommand command,
        DateTimeOffset receivedAt,
        int maxFutureClockSkewSeconds)
    {
        if (command.KioskId == Guid.Empty ||
            command.EndpointId == Guid.Empty ||
            command.CommandId == Guid.Empty ||
            command.SourceEventId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.ReportType) ||
            string.IsNullOrWhiteSpace(command.Status) ||
            command.SequenceNumber <= 0)
        {
            return "Kiosk, endpoint, command, source event, sequence, report type, and status are required.";
        }

        if (command.EdgeCreatedAt == default)
        {
            return "Edge-created timestamp is required.";
        }


        var latestAcceptedTimestamp = receivedAt.AddSeconds(maxFutureClockSkewSeconds);
        if (command.EdgeCreatedAt > latestAcceptedTimestamp ||
            command.ExecutorReportedAt > latestAcceptedTimestamp ||
            command.StockMovements.Any(item => item.OccurredAt > latestAcceptedTimestamp))
        {
            return "Execution report timestamps cannot exceed the allowed future clock skew.";
        }

        if (command.StockMovements.Count > 0 &&
            !string.Equals(command.ReportType, "ProductionExecution", StringComparison.OrdinalIgnoreCase))
        {
            return "Stock movement evidence is supported only for production execution reports.";
        }

        if (command.StockMovements.Count > 0 && !command.SourceProductionJobId.HasValue)
        {
            return "Stock movement evidence must be reported by a production job.";
        }

        if (command.StockMovements.Count > 100)
        {
            return "A production execution report supports at most 100 stock movement evidence items.";
        }

        if (command.StockMovements.Any(item =>
                item.SourceEventId == Guid.Empty ||
                item.IngredientDispenserStateId == Guid.Empty ||
                item.QuantityConsumed <= 0 ||
                item.BalanceAfter < 0) ||
            command.StockMovements.Select(item => item.SourceEventId).Distinct().Count() != command.StockMovements.Count)
        {
            return "Stock movement evidence requires unique event ids, dispenser states, positive consumed quantities, and non-negative balances.";
        }

        return null;
    }

    private static bool IsUsableEndpoint(KioskExecutionEndpoint? endpoint, IngestExecutionReportCommand command)
    {
        return endpoint is not null &&
            endpoint.KioskId == command.KioskId &&
            endpoint.Status == KioskExecutionEndpointStatus.Active &&
            endpoint.CredentialBinding is not null &&
            endpoint.CredentialBinding.Status == ExecutionEndpointCredentialBindingStatus.Active;
    }

    private static Guid? GetSourceExecutorId(KioskExecutionEndpoint endpoint)
    {
        return endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
            ? endpoint.FullEdgeRuntimeId
            : endpoint.ControllerId;
    }

    private async Task<bool> ApplyReportAsync(
        IngestExecutionReportCommand command,
        KioskExecutionEndpoint endpoint,
        Guid sourceExecutorId,
        EdgeCommand edgeCommand,
        DateTimeOffset executorReportedAt,
        DateTimeOffset cloudReceivedAt,
        ReportNotifications notifications,
        CancellationToken cancellationToken)
    {
        if (string.Equals(command.ReportType, "Deployment", StringComparison.OrdinalIgnoreCase))
        {
            return await ApplyDeploymentReportAsync(command, endpoint, cloudReceivedAt, cancellationToken);
        }

        if (string.Equals(command.ReportType, "ProductionExecution", StringComparison.OrdinalIgnoreCase))
        {
            return await ApplyProductionExecutionReportAsync(
                command,
                endpoint,
                sourceExecutorId,
                edgeCommand,
                executorReportedAt,
                cloudReceivedAt,
                notifications,
                cancellationToken);
        }

        throw new DomainRuleException("Unsupported execution report type.");
    }

    private async Task<bool> ApplyDeploymentReportAsync(
        IngestExecutionReportCommand command,
        KioskExecutionEndpoint endpoint,
        DateTimeOffset cloudReceivedAt,
        CancellationToken cancellationToken)
    {
        if (command.DeploymentId is null || command.DeploymentId == Guid.Empty)
        {
            throw new DomainRuleException("Deployment reports require deployment id.");
        }

        if (endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge)
        {
            var deployment = await _deploymentStore.GetFullEdgeDeploymentAsync(command.DeploymentId.Value, cancellationToken)
                ?? throw new DomainRuleException("Full Edge deployment not found.");

            if (deployment.KioskId != command.KioskId || deployment.KioskExecutionEndpointId != command.EndpointId)
            {
                throw new DomainRuleException("Deployment does not belong to the reporting endpoint.");
            }

            if (IsStatus(command.Status, "Installed"))
            {
                return deployment.MarkInstalled(command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt);
            }

            if (IsStatus(command.Status, "Active"))
            {
                var changed = deployment.MarkActive(command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt);
                endpoint.ApplyFullEdgeObservedActivation(
                    deployment.Id,
                    deployment.ConfigurationReleaseId,
                    deployment.ReleaseChecksum,
                    command.SourceEventId,
                    command.EdgeCreatedAt,
                    cloudReceivedAt);
                return changed;
            }

            if (IsStatus(command.Status, "Failed"))
            {
                return deployment.MarkFailed(
                    command.SourceEventId,
                    command.EdgeCreatedAt,
                    cloudReceivedAt,
                    RequiredErrorCode(command),
                    command.ErrorMessage);
            }
        }
        else
        {
            var deployment = await _deploymentStore.GetControllerArtifactSetDeploymentAsync(command.DeploymentId.Value, cancellationToken)
                ?? throw new DomainRuleException("Controller artifact-set deployment not found.");

            if (deployment.KioskId != command.KioskId || deployment.KioskExecutionEndpointId != command.EndpointId)
            {
                throw new DomainRuleException("Deployment does not belong to the reporting endpoint.");
            }

            if (IsStatus(command.Status, "Installed"))
            {
                return deployment.MarkInstalled(command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt);
            }

            if (IsStatus(command.Status, "Active"))
            {
                var changed = deployment.MarkActive(command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt);
                endpoint.ApplyLowCostObservedActivation(
                    deployment.Id,
                    deployment.SourceConfigurationReleaseId,
                    deployment.ReleaseChecksum,
                    deployment.ActiveSetVersion,
                    deployment.ActiveSetChecksum,
                    command.SourceEventId,
                    command.EdgeCreatedAt,
                    cloudReceivedAt);
                return changed;
            }

            if (IsStatus(command.Status, "Failed"))
            {
                return deployment.MarkFailed(
                    command.SourceEventId,
                    command.EdgeCreatedAt,
                    cloudReceivedAt,
                    RequiredErrorCode(command),
                    command.ErrorMessage);
            }
        }

        throw new DomainRuleException("Unsupported deployment report status.");
    }

    private async Task<bool> ApplyProductionExecutionReportAsync(
        IngestExecutionReportCommand command,
        KioskExecutionEndpoint endpoint,
        Guid sourceExecutorId,
        EdgeCommand edgeCommand,
        DateTimeOffset executorReportedAt,
        DateTimeOffset cloudReceivedAt,
        ReportNotifications notifications,
        CancellationToken cancellationToken)
    {
        if (edgeCommand.CommandType != EdgeCommandType.ExecuteOrder)
        {
            throw new DomainRuleException("Production execution reports require an execute-order command.");
        }


        ValidateProductionReleaseAgainstCommand(command, edgeCommand);

        var status = ParseProductionStatus(command.Status);
        var physicalOutputState = ToPhysicalOutputState(command.PhysicalOutputMayHaveOccurred);
        var productionApplied = false;
        if (command.SourceProductionJobId.HasValue)
        {
            var productionRecord = await _productionStore.GetProductionExecutionRecordAsync(
                edgeCommand.Id,
                command.SourceProductionJobId.Value,
                cancellationToken);

            productionApplied = productionRecord is null;
            if (productionRecord is null)
            {
                productionRecord = ProductionExecutionRecord.Create(
                    edgeCommand.Id,
                    endpoint.Id,
                    endpoint.ExecutionProfile,
                    sourceExecutorId,
                    command.SourceEventId,
                    command.SequenceNumber,
                    command.EdgeCreatedAt,
                    executorReportedAt,
                    cloudReceivedAt,
                    status,
                    physicalOutputState,
                    command.SourceProductionJobId.Value,
                    command.WorkcellId,
                    command.ControllerId,
                    command.ExecutionPlanChecksum,
                    command.ActiveSetVersion,
                    command.ActiveSetChecksum,
                    command.ErrorCode,
                    command.ErrorMessage);

                await _productionStore.AddProductionExecutionRecordAsync(productionRecord, cancellationToken);
            }
            else
            {
                productionApplied = productionRecord.ApplyObservation(
                    command.SourceEventId,
                    command.SequenceNumber,
                    command.EdgeCreatedAt,
                    executorReportedAt,
                    cloudReceivedAt,
                    status,
                    physicalOutputState,
                    command.ErrorCode,
                    command.ErrorMessage);
            }
        }

        var orderApplied = false;
        if (!command.SourceProductionJobId.HasValue)
        {
            orderApplied = await ApplyOrderExecutionRecordAsync(
                command,
                endpoint,
                sourceExecutorId,
                edgeCommand,
                executorReportedAt,
                cloudReceivedAt,
                status,
                cancellationToken);
        }

        if (orderApplied)
        {
            await ApplyOrderLifecycleAsync(
                command,
                edgeCommand,
                status,
                executorReportedAt,
                notifications,
                cancellationToken);
        }

        if (productionApplied && command.StockMovements.Count > 0)
        {
            await ApplyStockEvidenceAsync(
                command,
                endpoint,
                sourceExecutorId,
                edgeCommand,
                notifications,
                cancellationToken);
        }

        return productionApplied || orderApplied;
    }

    private async Task<bool> ApplyOrderExecutionRecordAsync(
        IngestExecutionReportCommand command,
        KioskExecutionEndpoint endpoint,
        Guid sourceExecutorId,
        EdgeCommand edgeCommand,
        DateTimeOffset executorReportedAt,
        DateTimeOffset cloudReceivedAt,
        ProductionExecutionStatus status,
        CancellationToken cancellationToken)
    {
        if (edgeCommand.OrderId is null || edgeCommand.DispatchAttemptNo is null)
        {
            return false;
        }

        if (command.SourceConfigurationReleaseId is null ||
            command.SourceConfigurationReleaseId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.ReleaseChecksum))
        {
            throw new DomainRuleException("Order execution reports require source configuration release and checksum.");
        }

        var customerStatus = MapCustomerStatus(status, command.PhysicalOutputMayHaveOccurred);
        var orderRecord = await _productionStore.GetOrderExecutionRecordAsync(edgeCommand.Id, cancellationToken);
        if (orderRecord is null)
        {
            orderRecord = OrderExecutionRecord.Create(
                edgeCommand.OrderId.Value,
                edgeCommand.Id,
                edgeCommand.DispatchAttemptNo.Value,
                endpoint.Id,
                endpoint.ExecutionProfile,
                sourceExecutorId,
                command.SourceConfigurationReleaseId.Value,
                command.ReleaseChecksum,
                command.SourceEventId,
                command.SequenceNumber,
                command.EdgeCreatedAt,
                executorReportedAt,
                cloudReceivedAt,
                status,
                ExecutionObservationStatus.Fresh,
                customerStatus);

            await _productionStore.AddOrderExecutionRecordAsync(orderRecord, cancellationToken);
            return true;
        }

        if (orderRecord.SourceConfigurationReleaseId != command.SourceConfigurationReleaseId.Value ||
            !string.Equals(orderRecord.ReleaseChecksum, command.ReleaseChecksum, StringComparison.Ordinal))
        {
            throw new DomainRuleException("Order execution report release does not match the dispatched command.");
        }

        return orderRecord.ApplyObservation(
            command.SourceEventId,
            command.SequenceNumber,
            command.EdgeCreatedAt,
            executorReportedAt,
            cloudReceivedAt,
            status,
            ExecutionObservationStatus.Fresh,
            customerStatus);
    }

    private async Task ApplyOrderLifecycleAsync(
        IngestExecutionReportCommand command,
        EdgeCommand edgeCommand,
        ProductionExecutionStatus status,
        DateTimeOffset executorReportedAt,
        ReportNotifications notifications,
        CancellationToken cancellationToken)
    {
        if (!edgeCommand.OrderId.HasValue)
        {
            return;
        }

        var order = await _productionStore.GetOrderAsync(edgeCommand.OrderId.Value, cancellationToken)
            ?? throw new DomainRuleException("Order for production execution report was not found.");
        var previousStatus = order.Status;

        switch (status)
        {
            case ProductionExecutionStatus.Accepted when order.Status == OrderStatus.ReadyForExecution:
                order.MarkAccepted();
                break;
            case ProductionExecutionStatus.Running when order.Status is OrderStatus.ReadyForExecution or OrderStatus.Accepted:
                order.MarkPreparing();
                break;
            case ProductionExecutionStatus.Completed when order.Status != OrderStatus.Completed:
                order.Complete(executorReportedAt);
                break;
            case ProductionExecutionStatus.Failed when order.Status != OrderStatus.Failed:
                order.MarkFailed(command.ErrorMessage);
                break;
            case ProductionExecutionStatus.RequiresManualIntervention when order.Status != OrderStatus.RefundRequired:
                order.MarkRefundRequired(command.ErrorMessage);
                break;
        }

        if (order.Status == previousStatus)
        {
            return;
        }

        await _productionStore.AddOrderStatusHistoryAsync(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = previousStatus,
            ToStatus = order.Status,
            ChangedAt = executorReportedAt,
            Reason = BuildOrderHistoryReason(command)
        }, cancellationToken);

        var projection = OrderStatusProjector.ProjectFromOrder(order);
        notifications.OrderStatusChanged = new OrderStatusChangedEvent
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            KioskId = order.KioskId,
            OrganizationId = order.OrganizationId,
            StoreId = order.StoreId,
            OldStatus = previousStatus.ToString(),
            NewStatus = order.Status.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            CustomerStatus = projection.CustomerStatus,
            CustomerStatusMessage = projection.CustomerStatusMessage,
            CanRetryPayment = projection.CanRetryPayment,
            RequiresStaffSupport = projection.RequiresStaffSupport,
            UpdatedAt = executorReportedAt,
            Version = 1
        };
    }

    private async Task ApplyStockEvidenceAsync(
        IngestExecutionReportCommand command,
        KioskExecutionEndpoint endpoint,
        Guid sourceExecutorId,
        EdgeCommand edgeCommand,
        ReportNotifications notifications,
        CancellationToken cancellationToken)
    {
        foreach (var evidence in command.StockMovements)
        {
            if (await _stockStore.StockMovementExistsAsync(evidence.SourceEventId, cancellationToken))
            {
                continue;
            }

            var state = await _stockStore.GetDispenserStateAsync(
                evidence.IngredientDispenserStateId,
                cancellationToken)
                ?? throw new DomainRuleException("Stock movement dispenser state was not found.");
            if (state.KioskId != endpoint.KioskId || state.Kiosk is null)
            {
                throw new DomainRuleException("Stock movement dispenser state does not belong to the reporting kiosk.");
            }

            var occurredAt = evidence.OccurredAt ?? command.EdgeCreatedAt;
            if (evidence.BalanceAfter.HasValue)
            {
                state.RecordSensorLevel(
                    state.CurrentLevelStatus,
                    occurredAt,
                    estimatedQuantity: evidence.BalanceAfter.Value);
            }

            var movement = StockMovement.Create(
                state.Id,
                state.Kiosk.OrganizationId,
                state.Kiosk.StoreId,
                state.KioskId,
                state.DeviceId,
                state.IngredientId,
                "CONSUME",
                -evidence.QuantityConsumed,
                evidence.BalanceAfter,
                state.Unit,
                occurredAt,
                "PRODUCTION_EXECUTION",
                "Order",
                edgeCommand.OrderId,
                evidence.SourceEventId,
                evidence.IsEstimated);
            movement.OriginNodeId = sourceExecutorId;
            movement.Version = command.SequenceNumber;
            movement.CorrelationId = edgeCommand.OrderId;
            movement.CausationId = command.SourceEventId;

            await _stockStore.AddStockMovementAsync(movement, cancellationToken);
            notifications.InventoryChanged.Add(new InventoryChangedEvent
            {
                DispenserStateId = state.Id,
                KioskId = state.KioskId!.Value,
                OrganizationId = state.Kiosk.OrganizationId,
                StoreId = state.Kiosk.StoreId,
                IngredientName = state.Ingredient.Name,
                EstimatedQuantity = state.EstimatedQuantity ?? evidence.BalanceAfter ?? 0,
                Unit = state.Unit,
                Status = state.CurrentLevelStatus.ToString(),
                UpdatedAt = occurredAt,
                Version = 1
            });
        }
    }

    private async Task PublishNotificationsAsync(
        ReportNotifications notifications,
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

    private static string BuildOrderHistoryReason(IngestExecutionReportCommand command)
    {
        var error = string.IsNullOrWhiteSpace(command.ErrorCode)
            ? null
            : $" Error: {command.ErrorCode.Trim()}.";
        return $"Production execution report: {command.Status.Trim()}.{error}".Trim();
    }

    private static SyncEventInbox BuildInboxEvent(
        IngestExecutionReportCommand command,
        Guid sourceExecutorId,
        DateTimeOffset cloudReceivedAt)
    {
        return new SyncEventInbox
        {
            EventId = command.SourceEventId,
            KioskId = command.KioskId,
            SourceNodeId = sourceExecutorId,
            CausationId = command.CommandId,
            EventType = $"ExecutionReport.{command.ReportType.Trim()}",
            AggregateType = "EdgeCommand",
            AggregateId = command.CommandId,
            PayloadJson = JsonSerializer.Serialize(new
            {
                command.CommandId,
                command.ReportType,
                command.Status,
                command.SequenceNumber,
                command.EdgeCreatedAt,
                command.ExecutorReportedAt,
                command.DeploymentId,
                command.SourceProductionJobId,
                command.WorkcellId,
                command.ControllerId,
                command.ExecutionPlanChecksum,
                command.ActiveSetVersion,
                command.ActiveSetChecksum,
                command.SourceConfigurationReleaseId,
                command.ReleaseChecksum,
                command.PhysicalOutputMayHaveOccurred,
                command.ErrorCode,
                command.ErrorMessage,
                command.PayloadJson,
                command.StockMovements
            }),
            OccurredAt = command.EdgeCreatedAt,
            ReceivedAt = cloudReceivedAt
        };
    }

    private static bool MatchesReportIdentity(SyncEventInbox existing, SyncEventInbox candidate)
    {
        return existing.KioskId == candidate.KioskId &&
               existing.CausationId == candidate.CausationId &&
               existing.AggregateType == candidate.AggregateType &&
               existing.AggregateId == candidate.AggregateId &&
               string.Equals(existing.EventType, candidate.EventType, StringComparison.Ordinal) &&
               string.Equals(existing.PayloadJson, candidate.PayloadJson, StringComparison.Ordinal);
    }

    private static ExecutionReportIngestResult BuildResult(
        IngestExecutionReportCommand command,
        bool applied,
        bool duplicate)
    {
        return new ExecutionReportIngestResult
        {
            CommandId = command.CommandId,
            SourceEventId = command.SourceEventId,
            ReportType = command.ReportType.Trim(),
            Status = command.Status.Trim(),
            Applied = applied,
            Duplicate = duplicate
        };
    }

    private static bool IsStatus(string status, string expected)
    {
        return string.Equals(status.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }

    private static string RequiredErrorCode(IngestExecutionReportCommand command)
    {
        return string.IsNullOrWhiteSpace(command.ErrorCode) ? "ExecutorReportedFailure" : command.ErrorCode.Trim();
    }

    private static ProductionExecutionStatus ParseProductionStatus(string status)
    {
        return Enum.TryParse<ProductionExecutionStatus>(status.Trim(), ignoreCase: true, out var parsed)
            ? parsed
            : throw new DomainRuleException("Unsupported production execution status.");
    }

    private static PhysicalOutputState ToPhysicalOutputState(bool? physicalOutputMayHaveOccurred)
    {
        return physicalOutputMayHaveOccurred switch
        {
            true => PhysicalOutputState.Yes,
            false => PhysicalOutputState.No,
            _ => PhysicalOutputState.Unknown
        };
    }

    private static CustomerExecutionStatus MapCustomerStatus(
        ProductionExecutionStatus status,
        bool? physicalOutputMayHaveOccurred)
    {
        return status switch
        {
            ProductionExecutionStatus.Accepted or ProductionExecutionStatus.Running => CustomerExecutionStatus.Processing,
            ProductionExecutionStatus.Completed => CustomerExecutionStatus.Completed,
            ProductionExecutionStatus.Failed when physicalOutputMayHaveOccurred == true => CustomerExecutionStatus.SupportRequired,
            ProductionExecutionStatus.Failed => CustomerExecutionStatus.Failed,
            ProductionExecutionStatus.RequiresManualIntervention => CustomerExecutionStatus.SupportRequired,
            _ => CustomerExecutionStatus.ExecutionUnconfirmed
        };
    }

    private static void ValidateProductionReleaseAgainstCommand(
        IngestExecutionReportCommand command,
        EdgeCommand edgeCommand)
    {
        if (!command.SourceConfigurationReleaseId.HasValue ||
            command.SourceConfigurationReleaseId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.ReleaseChecksum))
        {
            throw new DomainRuleException("Production execution reports require source configuration release and checksum.");
        }

        try
        {
            using var payload = JsonDocument.Parse(edgeCommand.PayloadJson);
            var root = payload.RootElement;
            if (!root.TryGetProperty("ConfigurationReleaseId", out var releaseIdElement) ||
                !releaseIdElement.TryGetGuid(out var dispatchedReleaseId) ||
                !root.TryGetProperty("ReleaseChecksum", out var checksumElement))
            {
                throw new DomainRuleException("Execute-order command payload is missing release provenance.");
            }

            var dispatchedChecksum = checksumElement.GetString();
            if (command.SourceConfigurationReleaseId.Value != dispatchedReleaseId ||
                !string.Equals(command.ReleaseChecksum.Trim(), dispatchedChecksum, StringComparison.Ordinal))
            {
                throw new DomainRuleException("Production execution report release does not match the dispatched command.");
            }
        }
        catch (JsonException ex)
        {
            throw new DomainRuleException($"Execute-order command payload is invalid: {ex.Message}");
        }
    }

    private sealed class ReportNotifications
    {
        public OrderStatusChangedEvent? OrderStatusChanged { get; set; }
        public List<InventoryChangedEvent> InventoryChanged { get; } = [];
    }
}
