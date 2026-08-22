using System.Text.Json;
using Application.Devices.Connectivity.Commands;
using Application.Devices.Connectivity.Contracts;
using Application.Devices.Telemetry.Commands;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Reports.Commands;
using Application.Inventory.Observations;
using Application.Shared.Wrappers;
using Application.Sync.Ingestion.Commands;

namespace Application.EdgeIntegration.Uplink;

public interface IEdgeUplinkMessageDispatcher
{
    Task<EdgeUplinkResult> DispatchAsync(
        Guid endpointId,
        string messageType,
        EdgeUplinkEnvelope envelope,
        CancellationToken cancellationToken);
}

public sealed class EdgeUplinkMessageDispatcher(
    IExecutionEndpointTransportAuthStore endpointStore,
    IngestKioskHeartbeatCommandHandler heartbeatHandler,
    IngestBatchEventsCommandHandler telemetryHandler,
    IngestExecutionReadinessCommandHandler readinessHandler,
    ReplaceExecutionEndpointReportedDevicesCommandHandler reportedDevicesHandler,
    IngestExecutionReportCommandHandler executionReportHandler,
    IngestProductionEventsBatchCommandHandler productionEventsHandler,
    IngestEdgeStateSummariesCommandHandler stateSummariesHandler,
    IngestInventorySensorObservationsCommandHandler inventoryObservationsHandler) : IEdgeUplinkMessageDispatcher
{
    public async Task<EdgeUplinkResult> DispatchAsync(
        Guid endpointId,
        string messageType,
        EdgeUplinkEnvelope envelope,
        CancellationToken cancellationToken)
    {
        if (endpointId == Guid.Empty || envelope.MessageId == Guid.Empty)
            return Rejected(endpointId, messageType, envelope.MessageId, 400, "Endpoint and message id are required.");
        if (envelope.SentAt == default)
            return Rejected(endpointId, messageType, envelope.MessageId, 400, "MQTT uplink sent timestamp is required.");
        if (envelope.SchemaVersion != 1)
            return Rejected(endpointId, messageType, envelope.MessageId, 400, "Unsupported MQTT uplink schema version.");
        if (!EdgeUplinkMessageTypes.All.Contains(messageType))
            return Rejected(endpointId, messageType, envelope.MessageId, 404, "Unknown MQTT uplink message type.");

        var endpoint = await endpointStore.GetEndpointAsync(endpointId, cancellationToken);
        if (endpoint is null)
            return Rejected(endpointId, messageType, envelope.MessageId, 403, "Execution endpoint was not found.");

        try
        {
            return messageType switch
            {
                EdgeUplinkMessageTypes.Heartbeat => FromApiResult(
                    endpointId, messageType, envelope.MessageId,
                    await heartbeatHandler.HandleAsync(
                        MapHeartbeat(endpoint.KioskId, endpointId, Deserialize<EdgeHeartbeatUplink>(envelope)),
                        cancellationToken)),
                EdgeUplinkMessageTypes.TelemetryEvents => FromApiResult(
                    endpointId, messageType, envelope.MessageId,
                    await telemetryHandler.HandleAsync(
                        MapTelemetry(endpoint.KioskId, endpointId, Deserialize<EdgeTelemetryBatchUplink>(envelope)),
                        cancellationToken)),
                EdgeUplinkMessageTypes.Readiness => FromApiResult(
                    endpointId, messageType, envelope.MessageId,
                    await readinessHandler.HandleAsync(
                        MapReadiness(endpoint.KioskId, endpointId, Deserialize<EdgeReadinessUplink>(envelope)),
                        cancellationToken)),
                EdgeUplinkMessageTypes.ReportedDevices => FromApiResult(
                    endpointId, messageType, envelope.MessageId,
                    await reportedDevicesHandler.HandleAsync(
                        MapReportedDevices(endpoint.KioskId, endpointId, Deserialize<EdgeReportedDevicesUplink>(envelope)),
                        cancellationToken)),
                EdgeUplinkMessageTypes.ExecutionReport => FromApiResult(
                    endpointId, messageType, envelope.MessageId,
                    await executionReportHandler.HandleAsync(
                        MapExecutionReport(endpoint.KioskId, endpointId, Deserialize<EdgeExecutionReportUplink>(envelope)),
                        cancellationToken)),
                EdgeUplinkMessageTypes.ProductionEvents => FromApiResult(
                    endpointId, messageType, envelope.MessageId,
                    await productionEventsHandler.HandleAsync(
                        MapProductionEvents(endpoint.KioskId, endpointId, Deserialize<EdgeProductionEventsUplink>(envelope)),
                        cancellationToken)),
                EdgeUplinkMessageTypes.StateSummaries => FromApiResult(
                    endpointId, messageType, envelope.MessageId,
                    await stateSummariesHandler.HandleAsync(
                        MapStateSummaries(endpoint.KioskId, endpointId, Deserialize<EdgeStateSummariesUplink>(envelope)),
                        cancellationToken)),
                EdgeUplinkMessageTypes.InventoryObservations => FromApiResult(
                    endpointId, messageType, envelope.MessageId,
                    await inventoryObservationsHandler.HandleAsync(
                        MapInventoryObservations(endpoint.KioskId, endpointId, Deserialize<EdgeInventoryObservationsUplink>(envelope)),
                        cancellationToken)),
                _ => Rejected(endpointId, messageType, envelope.MessageId, 404, "Unknown MQTT uplink message type.")
            };
        }
        catch (JsonException ex)
        {
            return Rejected(endpointId, messageType, envelope.MessageId, 400, ex.Message);
        }
    }

    private static readonly JsonSerializerOptions SerializerOptions = EdgeUplinkJson.CreateSerializerOptions();

    private static T Deserialize<T>(EdgeUplinkEnvelope envelope) =>
        envelope.Payload.Deserialize<T>(SerializerOptions)
        ?? throw new JsonException($"MQTT uplink payload for {typeof(T).Name} is required.");

    private static EdgeUplinkResult FromApiResult<T>(
        Guid endpointId,
        string messageType,
        Guid messageId,
        ApiResult<T> result) => new()
        {
            MessageId = messageId,
            EndpointId = endpointId,
            MessageType = messageType,
            ProcessedAt = DateTimeOffset.UtcNow,
            Succeeded = result.Succeeded,
            StatusCode = result.StatusCode,
            Retryable = !result.Succeeded && IsRetryable(result.StatusCode),
            Message = result.Message,
            Data = result.Data
        };

    private static EdgeUplinkResult Rejected(
        Guid endpointId,
        string messageType,
        Guid messageId,
        int statusCode,
        string message) => new()
        {
            MessageId = messageId,
            EndpointId = endpointId,
            MessageType = messageType,
            ProcessedAt = DateTimeOffset.UtcNow,
            Succeeded = false,
            StatusCode = statusCode,
            Retryable = IsRetryable(statusCode),
            Message = message
        };

    private static bool IsRetryable(int statusCode) =>
        statusCode is 408 or 425 or 429 || statusCode >= 500;

    private static IngestKioskHeartbeatCommand MapHeartbeat(
        Guid kioskId,
        Guid endpointId,
        EdgeHeartbeatUplink payload) => new()
        {
            KioskId = kioskId,
            EndpointId = endpointId,
            OriginNodeId = payload.OriginNodeId,
            HeartbeatSequence = payload.HeartbeatSequence,
            ReportedAt = payload.ReportedAt,
            Status = payload.Status,
            RobotStatus = payload.RobotStatus,
            NetworkStatus = payload.NetworkStatus,
            AppVersion = payload.AppVersion,
            FirmwareVersion = payload.FirmwareVersion,
            CpuUsagePercent = payload.CpuUsagePercent,
            MemoryUsagePercent = payload.MemoryUsagePercent,
            DiskUsagePercent = payload.DiskUsagePercent,
            PendingSyncEventCount = payload.PendingSyncEventCount
        };

    private static IngestBatchEventsCommand MapTelemetry(
        Guid kioskId,
        Guid endpointId,
        EdgeTelemetryBatchUplink payload) => new()
        {
            KioskId = kioskId,
            EndpointId = endpointId,
            OriginNodeId = payload.OriginNodeId,
            Events = payload.Events.Select(item => new BatchSyncEventItem
            {
                EventId = item.EventId,
                EventType = item.EventType switch
                {
                    EdgeTelemetryEventType.Heartbeat => BatchSyncEventType.Heartbeat,
                    EdgeTelemetryEventType.DeviceEvent => BatchSyncEventType.DeviceEvent,
                    EdgeTelemetryEventType.LocalLog => BatchSyncEventType.LocalLog,
                    _ => throw new JsonException("Unsupported telemetry event type.")
                },
                Heartbeat = item.Heartbeat is null ? null : new BatchHeartbeatData
                {
                    HeartbeatSequence = item.Heartbeat.HeartbeatSequence,
                    ReportedAt = item.Heartbeat.ReportedAt,
                    Status = item.Heartbeat.Status,
                    RobotStatus = item.Heartbeat.RobotStatus,
                    NetworkStatus = item.Heartbeat.NetworkStatus,
                    AppVersion = item.Heartbeat.AppVersion,
                    FirmwareVersion = item.Heartbeat.FirmwareVersion,
                    CpuUsagePercent = item.Heartbeat.CpuUsagePercent,
                    MemoryUsagePercent = item.Heartbeat.MemoryUsagePercent,
                    DiskUsagePercent = item.Heartbeat.DiskUsagePercent,
                    PendingSyncEventCount = item.Heartbeat.PendingSyncEventCount
                },
                DeviceEvent = item.DeviceEvent is null ? null : new BatchDeviceEventData
                {
                    DeviceId = item.DeviceEvent.DeviceId,
                    CorrelationId = item.DeviceEvent.CorrelationId,
                    CausationId = item.DeviceEvent.CausationId,
                    EventType = item.DeviceEvent.EventType,
                    Severity = item.DeviceEvent.Severity,
                    Message = item.DeviceEvent.Message,
                    OccurredAt = item.DeviceEvent.OccurredAt,
                    PayloadJson = item.DeviceEvent.Payload?.GetRawText()
                },
                LocalLog = item.LocalLog is null ? null : new BatchLocalLogData
                {
                    DeviceId = item.LocalLog.DeviceId,
                    OrderId = item.LocalLog.OrderId,
                    CorrelationId = item.LocalLog.CorrelationId,
                    CausationId = item.LocalLog.CausationId,
                    Action = item.LocalLog.Action,
                    Category = item.LocalLog.Category,
                    Severity = item.LocalLog.Severity,
                    Message = item.LocalLog.Message,
                    OccurredAt = item.LocalLog.OccurredAt,
                    PayloadJson = item.LocalLog.Payload?.GetRawText()
                }
            }).ToArray()
        };

    private static IngestExecutionReadinessCommand MapReadiness(
        Guid kioskId,
        Guid endpointId,
        EdgeReadinessUplink payload)
    {
        if (payload.LocalPersistenceHealth is null)
            throw new JsonException("Local persistence health is required.");

        return new IngestExecutionReadinessCommand
        {
            KioskId = kioskId,
            EndpointId = endpointId,
            SourceExecutorId = payload.SourceExecutorId,
            StateRevision = payload.StateRevision,
            ExecutorReportedAt = payload.ExecutorReportedAt,
            Readiness = payload.Readiness,
            Activity = payload.Activity,
            Safety = payload.Safety,
            CurrentCommandId = payload.CurrentCommandId,
            PhysicalOutputState = payload.PhysicalOutputState,
            FaultCode = payload.FaultCode,
            LocalPersistenceHealth = new LocalPersistenceHealthInput(
                payload.LocalPersistenceHealth.StorageWritable,
                payload.LocalPersistenceHealth.FreeSpaceBytes,
                payload.LocalPersistenceHealth.MinimumRequiredFreeSpaceBytes,
                payload.LocalPersistenceHealth.LocalDatabaseHealth,
                payload.LocalPersistenceHealth.PendingEventCount,
                payload.LocalPersistenceHealth.MaximumPendingEventCount),
            Capabilities = payload.Capabilities.Select(item => new ExecutionCapabilityInput(
                item.CapabilityCode,
                item.WorkcellCode,
                item.IsAvailable,
                item.UnavailableReason)).ToArray()
        };
    }

    private static ReplaceExecutionEndpointReportedDevicesCommand MapReportedDevices(
        Guid kioskId,
        Guid endpointId,
        EdgeReportedDevicesUplink payload) => new()
        {
            KioskId = kioskId,
            EndpointId = endpointId,
            SourceExecutorId = payload.SourceExecutorId,
            SnapshotRevision = payload.SnapshotRevision,
            ObservedAt = payload.ObservedAt,
            Devices = payload.Devices.Select(item => new Domain.Devices.ExecutionEndpoints.ReportedDeviceSnapshotItem(
                item.SourceDeviceKey,
                item.DeviceId,
                item.RuntimeTargetCode,
                item.MachineModelCode)).ToArray()
        };

    private static IngestExecutionReportCommand MapExecutionReport(
        Guid kioskId,
        Guid endpointId,
        EdgeExecutionReportUplink payload) => new()
        {
            KioskId = kioskId,
            EndpointId = endpointId,
            CommandId = payload.CommandId,
            SourceEventId = payload.SourceEventId,
            SequenceNumber = payload.SequenceNumber,
            EdgeCreatedAt = payload.EdgeCreatedAt,
            ExecutorReportedAt = payload.ExecutorReportedAt,
            ReportType = payload.ReportType,
            Status = payload.Status,
            DeploymentId = payload.DeploymentId,
            SourceProductionJobId = payload.SourceProductionJobId,
            OrderItemId = payload.OrderItemId,
            ProductionUnitNo = payload.ProductionUnitNo,
            ProductionUnitQuantity = payload.ProductionUnitQuantity,
            WorkcellId = payload.WorkcellId,
            ControllerId = payload.ControllerId,
            ExecutionPlanChecksum = payload.ExecutionPlanChecksum,
            ActiveSetVersion = payload.ActiveSetVersion,
            ActiveSetChecksum = payload.ActiveSetChecksum,
            SourceConfigurationReleaseId = payload.SourceConfigurationReleaseId,
            ReleaseChecksum = payload.ReleaseChecksum,
            PhysicalOutputMayHaveOccurred = payload.PhysicalOutputMayHaveOccurred,
            ErrorCode = payload.ErrorCode,
            ErrorMessage = payload.ErrorMessage,
            PayloadJson = payload.PayloadJson,
            StockMovements = payload.StockMovements.Select(item => new StockMovementEvidenceInput(
                item.SourceEventId,
                item.IngredientDispenserStateId,
                item.QuantityConsumed,
                item.BalanceAfter,
                item.OccurredAt,
                item.IsEstimated,
                item.OrderItemId)).ToArray()
        };

    private static IngestProductionEventsBatchCommand MapProductionEvents(
        Guid kioskId,
        Guid endpointId,
        EdgeProductionEventsUplink payload) => new()
        {
            KioskId = kioskId,
            EndpointId = endpointId,
            SourceExecutorId = payload.SourceExecutorId,
            Events = payload.Events.Select(item => new ProductionEventBatchItem
            {
                EventId = item.EventId,
                SequenceNumber = item.SequenceNumber,
                EventType = item.EventType,
                SchemaVersion = item.SchemaVersion,
                EdgeCreatedAt = item.EdgeCreatedAt,
                OrderId = item.OrderId,
                SourceCommandId = item.SourceCommandId,
                ProductionJobId = item.ProductionJobId,
                PayloadJson = item.Payload?.GetRawText()
            }).ToArray()
        };

    private static IngestEdgeStateSummariesCommand MapStateSummaries(
        Guid kioskId,
        Guid endpointId,
        EdgeStateSummariesUplink payload) => new()
        {
            KioskId = kioskId,
            EndpointId = endpointId,
            SourceExecutorId = payload.SourceExecutorId,
            Summaries = payload.Summaries.Select(item => new EdgeStateSummaryItem
            {
                SummaryKind = item.SummaryKind,
                StateRevision = item.StateRevision,
                SummarySchemaVersion = item.SummarySchemaVersion,
                EdgeCreatedAt = item.EdgeCreatedAt,
                PayloadJson = item.Payload.GetRawText()
            }).ToArray()
        };

    private static IngestInventorySensorObservationsCommand MapInventoryObservations(
        Guid kioskId,
        Guid endpointId,
        EdgeInventoryObservationsUplink payload) => new()
        {
            KioskId = kioskId,
            EndpointId = endpointId,
            SourceExecutorId = payload.SourceExecutorId,
            Observations = payload.Observations.Select(item => new InventorySensorObservationInput
            {
                SourceEventId = item.SourceEventId,
                IngredientDispenserStateId = item.IngredientDispenserStateId,
                DeviceId = item.DeviceId,
                ObservationSequence = item.ObservationSequence,
                ObservedLevelStatus = item.ObservedLevelStatus,
                ObservedAt = item.ObservedAt,
                SensorPayload = item.SensorPayload
            }).ToArray()
        };
}
