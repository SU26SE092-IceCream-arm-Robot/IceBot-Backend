using Domain.Devices.Telemetry;
using Application.Devices.Telemetry.Abstractions;
using Application.Devices.Telemetry.Results;
using Application.Shared.Wrappers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Devices.Telemetry.Commands;

public sealed class IngestBatchEventsCommandHandler
{
    private readonly IBatchEventSyncStore _receiptStore;
    private readonly IngestKioskHeartbeatCommandHandler _heartbeatHandler;
    private readonly IngestDeviceEventCommandHandler _deviceEventHandler;
    private readonly IngestLocalOperationLogCommandHandler _localLogHandler;
    private readonly EdgeTelemetryIngestionOptions _options;
    private readonly ILogger<IngestBatchEventsCommandHandler> _logger;

    public IngestBatchEventsCommandHandler(
        IBatchEventSyncStore receiptStore,
        IngestKioskHeartbeatCommandHandler heartbeatHandler,
        IngestDeviceEventCommandHandler deviceEventHandler,
        IngestLocalOperationLogCommandHandler localLogHandler,
        IOptions<EdgeTelemetryIngestionOptions> options,
        ILogger<IngestBatchEventsCommandHandler> logger)
    {
        _receiptStore = receiptStore;
        _heartbeatHandler = heartbeatHandler;
        _deviceEventHandler = deviceEventHandler;
        _localLogHandler = localLogHandler;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ApiResult<BatchEventSyncResult>> HandleAsync(
        IngestBatchEventsCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.KioskId == Guid.Empty || command.EndpointId == Guid.Empty || command.OriginNodeId == Guid.Empty)
        {
            return ApiResult<BatchEventSyncResult>.Fail("Kiosk, endpoint, and origin node are required.", 400);
        }

        if (command.Events.Count is 0 || command.Events.Count > _options.MaxBatchEventCount)
        {
            return ApiResult<BatchEventSyncResult>.Fail(
                $"Batch must contain between 1 and {_options.MaxBatchEventCount} events.", 400);
        }

        if (command.Events.Any(item => item.EventId == Guid.Empty) ||
            command.Events.Select(item => item.EventId).Distinct().Count() != command.Events.Count)
        {
            return ApiResult<BatchEventSyncResult>.Fail("Batch event ids must be non-empty and unique within the request.", 400);
        }

        var results = new List<BatchEventSyncItemResult>(command.Events.Count);
        foreach (var item in command.Events)
        {
            results.Add(await ProcessItemAsync(command, item, cancellationToken));
        }

        var accepted = results.Count(item => item.Status == "Accepted");
        var duplicates = results.Count(item => item.Status == "Duplicate");
        var rejected = results.Count - accepted - duplicates;
        var data = new BatchEventSyncResult
        {
            AcceptedCount = accepted,
            DuplicateCount = duplicates,
            RejectedCount = rejected,
            Items = results
        };
        return ApiResult<BatchEventSyncResult>.Success(
            data,
            rejected == 0 ? "Batch events processed." : "Batch processed with item failures.",
            rejected == 0 ? 200 : 207);
    }

    private async Task<BatchEventSyncItemResult> ProcessItemAsync(
        IngestBatchEventsCommand command,
        BatchSyncEventItem item,
        CancellationToken cancellationToken)
    {
        try
        {
            if (await _receiptStore.GetReceiptAsync(command.OriginNodeId, item.EventId, cancellationToken) is not null)
            {
                return ToItem(item, "Duplicate", 200, "Batch event already processed.");
            }

            return item.EventType switch
            {
                BatchSyncEventType.Heartbeat => await ProcessHeartbeatAsync(command, item, cancellationToken),
                BatchSyncEventType.DeviceEvent => await ProcessDeviceEventAsync(command, item, cancellationToken),
                BatchSyncEventType.LocalLog => await ProcessLocalLogAsync(command, item, cancellationToken),
                _ => ToItem(item, "Rejected", 400, "Unsupported batch event type.")
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Batch event {EventId} ({EventType}) failed.", item.EventId, item.EventType);
            return ToItem(item, "Failed", 500, "Batch event processing failed.");
        }
    }

    private async Task<BatchEventSyncItemResult> ProcessHeartbeatAsync(
        IngestBatchEventsCommand command,
        BatchSyncEventItem item,
        CancellationToken cancellationToken)
    {
        if (item.Heartbeat is null || item.DeviceEvent is not null || item.LocalLog is not null)
        {
            return ToItem(item, "Rejected", 400, "Heartbeat event requires only the heartbeat payload.");
        }

        var data = item.Heartbeat;
        var result = await _heartbeatHandler.HandleAsync(new IngestKioskHeartbeatCommand
        {
            KioskId = command.KioskId,
            EndpointId = command.EndpointId,
            OriginNodeId = command.OriginNodeId,
            HeartbeatSequence = data.HeartbeatSequence,
            ReportedAt = data.ReportedAt,
            Status = data.Status,
            RobotStatus = data.RobotStatus,
            NetworkStatus = data.NetworkStatus,
            AppVersion = data.AppVersion,
            FirmwareVersion = data.FirmwareVersion,
            CpuUsagePercent = data.CpuUsagePercent,
            MemoryUsagePercent = data.MemoryUsagePercent,
            DiskUsagePercent = data.DiskUsagePercent,
            PendingSyncEventCount = data.PendingSyncEventCount
        }, cancellationToken);
        if (!result.Succeeded)
        {
            return ToItem(item, "Rejected", result.StatusCode, result.Message);
        }

        var duplicateReceipt = await _receiptStore.RecordProcessedReceiptAsync(
            item.EventId, command.KioskId, command.OriginNodeId, item.EventType.ToString(), data.ReportedAt,
            "KioskHeartbeat", result.Data!.HeartbeatId, cancellationToken);
        return ToItem(item, result.Data.Duplicate || duplicateReceipt ? "Duplicate" : "Accepted",
            result.StatusCode, result.Message, result.Data.HeartbeatId);
    }

    private async Task<BatchEventSyncItemResult> ProcessDeviceEventAsync(
        IngestBatchEventsCommand command,
        BatchSyncEventItem item,
        CancellationToken cancellationToken)
    {
        if (item.DeviceEvent is null || item.Heartbeat is not null || item.LocalLog is not null)
        {
            return ToItem(item, "Rejected", 400, "DeviceEvent requires only the deviceEvent payload.");
        }

        var data = item.DeviceEvent;
        var result = await _deviceEventHandler.HandleAsync(new IngestDeviceEventCommand
        {
            KioskId = command.KioskId,
            EndpointId = command.EndpointId,
            OriginNodeId = command.OriginNodeId,
            DeviceId = data.DeviceId,
            EventId = item.EventId,
            CorrelationId = data.CorrelationId,
            CausationId = data.CausationId,
            EventType = data.EventType,
            Severity = data.Severity,
            Message = data.Message,
            OccurredAt = data.OccurredAt,
            PayloadJson = data.PayloadJson
        }, cancellationToken);
        if (!result.Succeeded)
        {
            return ToItem(item, "Rejected", result.StatusCode, result.Message);
        }

        var duplicateReceipt = await _receiptStore.RecordProcessedReceiptAsync(
            item.EventId, command.KioskId, command.OriginNodeId, item.EventType.ToString(), data.OccurredAt,
            "DeviceEvent", result.Data!.DeviceEventId, cancellationToken);
        return ToItem(item, result.Data.Duplicate || duplicateReceipt ? "Duplicate" : "Accepted",
            result.StatusCode, result.Message, result.Data.DeviceEventId);
    }

    private async Task<BatchEventSyncItemResult> ProcessLocalLogAsync(
        IngestBatchEventsCommand command,
        BatchSyncEventItem item,
        CancellationToken cancellationToken)
    {
        if (item.LocalLog is null || item.Heartbeat is not null || item.DeviceEvent is not null)
        {
            return ToItem(item, "Rejected", 400, "LocalLog requires only the localLog payload.");
        }

        var data = item.LocalLog;
        var result = await _localLogHandler.HandleAsync(new IngestLocalOperationLogCommand
        {
            KioskId = command.KioskId,
            EndpointId = command.EndpointId,
            OriginNodeId = command.OriginNodeId,
            SourceEventId = item.EventId,
            DeviceId = data.DeviceId,
            OrderId = data.OrderId,
            CorrelationId = data.CorrelationId,
            CausationId = data.CausationId,
            Action = data.Action,
            Category = data.Category,
            Severity = data.Severity,
            Message = data.Message,
            OccurredAt = data.OccurredAt,
            PayloadJson = data.PayloadJson
        }, cancellationToken);
        if (!result.Succeeded)
        {
            return ToItem(item, "Rejected", result.StatusCode, result.Message);
        }

        var duplicateReceipt = await _receiptStore.RecordProcessedReceiptAsync(
            item.EventId, command.KioskId, command.OriginNodeId, item.EventType.ToString(), data.OccurredAt,
            "OperationLog", result.Data!.OperationLogId, cancellationToken);
        return ToItem(item, result.Data.Duplicate || duplicateReceipt ? "Duplicate" : "Accepted",
            result.StatusCode, result.Message, result.Data.OperationLogId);
    }

    private static BatchEventSyncItemResult ToItem(
        BatchSyncEventItem item,
        string status,
        int statusCode,
        string? message,
        Guid? resourceId = null,
        long? acknowledgedSequenceNumber = null) => new()
        {
            EventId = item.EventId,
            EventType = item.EventType.ToString(),
            Status = status,
            StatusCode = statusCode,
            Message = message,
            ResourceId = resourceId,
            AcknowledgedSequenceNumber = acknowledgedSequenceNumber
        };
}
