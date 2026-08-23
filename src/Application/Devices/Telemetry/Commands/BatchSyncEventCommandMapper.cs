using System.Text.Json;
using Domain.Common.Enums;
using Domain.Devices.Telemetry;

namespace Application.Devices.Telemetry.Commands;

public interface IBatchSyncEventPayload
{
    Guid EventId { get; }
    BatchSyncEventType EventType { get; }
    IBatchHeartbeatPayload? Heartbeat { get; }
    IBatchDeviceEventPayload? DeviceEvent { get; }
    IBatchLocalLogPayload? LocalLog { get; }
}

public interface IBatchHeartbeatPayload
{
    long HeartbeatSequence { get; }
    DateTimeOffset ReportedAt { get; }
    KioskHeartbeatStatus Status { get; }
    string? RobotStatus { get; }
    string? NetworkStatus { get; }
    string? AppVersion { get; }
    string? FirmwareVersion { get; }
    decimal? CpuUsagePercent { get; }
    decimal? MemoryUsagePercent { get; }
    decimal? DiskUsagePercent { get; }
    int PendingSyncEventCount { get; }
}

public interface IBatchDeviceEventPayload
{
    Guid DeviceId { get; }
    Guid? CorrelationId { get; }
    Guid? CausationId { get; }
    string EventType { get; }
    SeverityLevel Severity { get; }
    string Message { get; }
    DateTimeOffset OccurredAt { get; }
    JsonElement? Payload { get; }
}

public interface IBatchLocalLogPayload
{
    Guid? DeviceId { get; }
    Guid? OrderId { get; }
    Guid? CorrelationId { get; }
    Guid? CausationId { get; }
    string Action { get; }
    string Category { get; }
    SeverityLevel Severity { get; }
    string Message { get; }
    DateTimeOffset OccurredAt { get; }
    JsonElement? Payload { get; }
}

public static class BatchSyncEventCommandMapper
{
    public static BatchSyncEventItem Map(IBatchSyncEventPayload payload) => new()
    {
        EventId = payload.EventId,
        EventType = payload.EventType,
        Heartbeat = payload.Heartbeat is null ? null : new BatchHeartbeatData
        {
            HeartbeatSequence = payload.Heartbeat.HeartbeatSequence,
            ReportedAt = payload.Heartbeat.ReportedAt,
            Status = payload.Heartbeat.Status,
            RobotStatus = payload.Heartbeat.RobotStatus,
            NetworkStatus = payload.Heartbeat.NetworkStatus,
            AppVersion = payload.Heartbeat.AppVersion,
            FirmwareVersion = payload.Heartbeat.FirmwareVersion,
            CpuUsagePercent = payload.Heartbeat.CpuUsagePercent,
            MemoryUsagePercent = payload.Heartbeat.MemoryUsagePercent,
            DiskUsagePercent = payload.Heartbeat.DiskUsagePercent,
            PendingSyncEventCount = payload.Heartbeat.PendingSyncEventCount
        },
        DeviceEvent = payload.DeviceEvent is null ? null : new BatchDeviceEventData
        {
            DeviceId = payload.DeviceEvent.DeviceId,
            CorrelationId = payload.DeviceEvent.CorrelationId,
            CausationId = payload.DeviceEvent.CausationId,
            EventType = payload.DeviceEvent.EventType,
            Severity = payload.DeviceEvent.Severity,
            Message = payload.DeviceEvent.Message,
            OccurredAt = payload.DeviceEvent.OccurredAt,
            PayloadJson = payload.DeviceEvent.Payload?.GetRawText()
        },
        LocalLog = payload.LocalLog is null ? null : new BatchLocalLogData
        {
            DeviceId = payload.LocalLog.DeviceId,
            OrderId = payload.LocalLog.OrderId,
            CorrelationId = payload.LocalLog.CorrelationId,
            CausationId = payload.LocalLog.CausationId,
            Action = payload.LocalLog.Action,
            Category = payload.LocalLog.Category,
            Severity = payload.LocalLog.Severity,
            Message = payload.LocalLog.Message,
            OccurredAt = payload.LocalLog.OccurredAt,
            PayloadJson = payload.LocalLog.Payload?.GetRawText()
        }
    };
}
