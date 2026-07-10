using Domain.Devices.Telemetry;
using Application.Devices.Telemetry.Results;
using Domain.Devices.Catalog;

namespace Application.Devices.Telemetry.Mapping;

internal static class KioskTelemetryResultMapper
{
    public static KioskHeartbeatResult ToResult(KioskHeartbeat heartbeat)
    {
        return new KioskHeartbeatResult
        {
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
            EventType = deviceEvent.EventType,
            Severity = deviceEvent.Severity,
            Message = deviceEvent.Message,
            OccurredAt = deviceEvent.OccurredAt
        };
    }
}
