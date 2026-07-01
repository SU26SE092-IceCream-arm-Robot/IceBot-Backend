using Domain.Devices.Telemetry;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Application.Devices.Commands;
using Application.Shared.Wrappers;
using Asp.Versioning;
using Domain.Common.Enums;
using Domain.Devices.Enums;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Configuration.Security;

namespace WebAPI.Controllers.IoT;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/iot/execution-endpoints/{endpointId:guid}/telemetry-events")]
public sealed class BatchEventsController : ControllerBase
{
    private readonly IngestBatchEventsCommandHandler _handler;
    private readonly ExecutionEndpointRequestAuthenticator _authenticator;

    public BatchEventsController(
        IngestBatchEventsCommandHandler handler,
        ExecutionEndpointRequestAuthenticator authenticator)
    {
        _handler = handler;
        _authenticator = authenticator;
    }

    [HttpPost]
    public async Task<IActionResult> Ingest(
        Guid endpointId,
        [FromBody] BatchEventSyncRequest request,
        CancellationToken cancellationToken)
    {
        var authentication = await _authenticator.AuthenticateAsync(
            HttpContext, endpointId, cancellationToken);
        if (!authentication.Succeeded)
        {
            return Unauthorized(ApiResult<object>.Fail(authentication.Message, 401));
        }

        var result = await _handler.HandleAsync(new IngestBatchEventsCommand
        {
            KioskId = authentication.Endpoint!.KioskId,
            EndpointId = endpointId,
            OriginNodeId = request.OriginNodeId,
            Events = request.Events.Select(ToCommandItem).ToArray()
        }, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    private static BatchSyncEventItem ToCommandItem(BatchSyncEventRequestItem item) => new()
    {
        EventId = item.EventId,
        EventType = item.EventType switch
        {
            TelemetryBatchEventType.Heartbeat => BatchSyncEventType.Heartbeat,
            TelemetryBatchEventType.DeviceEvent => BatchSyncEventType.DeviceEvent,
            TelemetryBatchEventType.LocalLog => BatchSyncEventType.LocalLog,
            _ => throw new ArgumentOutOfRangeException(nameof(item.EventType))
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
    };
}

public sealed class BatchEventSyncRequest
{
    [Required]
    public Guid OriginNodeId { get; init; }

    [Required]
    [MinLength(1)]
    public IReadOnlyList<BatchSyncEventRequestItem> Events { get; init; } = [];
}

public sealed class BatchSyncEventRequestItem
{
    [Required]
    public Guid EventId { get; init; }

    public TelemetryBatchEventType EventType { get; init; }

    public BatchHeartbeatRequest? Heartbeat { get; init; }
    public BatchDeviceEventRequest? DeviceEvent { get; init; }
    public BatchLocalLogRequest? LocalLog { get; init; }
}

public enum TelemetryBatchEventType
{
    Heartbeat = 0,
    DeviceEvent = 1,
    LocalLog = 2
}

public sealed class BatchHeartbeatRequest
{
    [Range(1, long.MaxValue)]
    public long HeartbeatSequence { get; init; }
    [Required]
    public DateTimeOffset ReportedAt { get; init; }
    public KioskHeartbeatStatus Status { get; init; } = KioskHeartbeatStatus.Online;
    [StringLength(100)] public string? RobotStatus { get; init; }
    [StringLength(100)] public string? NetworkStatus { get; init; }
    [StringLength(100)] public string? AppVersion { get; init; }
    [StringLength(100)] public string? FirmwareVersion { get; init; }
    [Range(typeof(decimal), "0", "100")] public decimal? CpuUsagePercent { get; init; }
    [Range(typeof(decimal), "0", "100")] public decimal? MemoryUsagePercent { get; init; }
    [Range(typeof(decimal), "0", "100")] public decimal? DiskUsagePercent { get; init; }
    [Range(0, int.MaxValue)] public int PendingSyncEventCount { get; init; }
}

public sealed class BatchDeviceEventRequest
{
    [Required] public Guid DeviceId { get; init; }
    public Guid? CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    [Required, StringLength(100)] public string EventType { get; init; } = string.Empty;
    public SeverityLevel Severity { get; init; }
    [Required, StringLength(1000)] public string Message { get; init; } = string.Empty;
    [Required] public DateTimeOffset OccurredAt { get; init; }
    public JsonElement? Payload { get; init; }
}

public sealed class BatchLocalLogRequest
{
    public Guid? DeviceId { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    [Required, StringLength(500)] public string Action { get; init; } = string.Empty;
    [Required, StringLength(500)] public string Category { get; init; } = "System";
    public SeverityLevel Severity { get; init; } = SeverityLevel.Info;
    [Required, StringLength(500)] public string Message { get; init; } = string.Empty;
    [Required] public DateTimeOffset OccurredAt { get; init; }
    public JsonElement? Payload { get; init; }
}
