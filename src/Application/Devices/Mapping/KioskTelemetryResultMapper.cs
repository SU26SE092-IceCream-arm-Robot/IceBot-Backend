using Application.Devices.Results;
using Domain.Devices.Entities;

namespace Application.Devices.Mapping;

internal static class KioskTelemetryResultMapper
{
    public static KioskHeartbeatResult ToResult(KioskHeartbeat heartbeat)
    {
        return new KioskHeartbeatResult
        {
            Id = heartbeat.Id,
            KioskId = heartbeat.KioskId,
            NodeId = heartbeat.NodeId,
            OriginNodeId = heartbeat.OriginNodeId,
            HeartbeatSequence = heartbeat.HeartbeatSequence,
            ReportedAt = heartbeat.ReportedAt,
            ReceivedAt = heartbeat.ReceivedAt,
            Status = heartbeat.Status,
            RobotStatus = heartbeat.RobotStatus,
            NetworkStatus = heartbeat.NetworkStatus,
            AppVersion = heartbeat.AppVersion,
            FirmwareVersion = heartbeat.FirmwareVersion,
            CpuUsagePercent = heartbeat.CpuUsagePercent,
            MemoryUsagePercent = heartbeat.MemoryUsagePercent,
            DiskUsagePercent = heartbeat.DiskUsagePercent,
            PendingSyncEventCount = heartbeat.PendingSyncEventCount
        };
    }

    public static DeviceEventResult ToResult(DeviceEvent deviceEvent)
    {
        return new DeviceEventResult
        {
            Id = deviceEvent.Id,
            DeviceId = deviceEvent.DeviceId,
            KioskId = deviceEvent.KioskId,
            EventId = deviceEvent.EventId,
            OriginNodeId = deviceEvent.OriginNodeId,
            CorrelationId = deviceEvent.CorrelationId,
            CausationId = deviceEvent.CausationId,
            EventType = deviceEvent.EventType,
            Severity = deviceEvent.Severity,
            Message = deviceEvent.Message,
            OccurredAt = deviceEvent.OccurredAt
        };
    }
}
