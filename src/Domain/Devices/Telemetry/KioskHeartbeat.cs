using Domain.Common;
using Domain.Devices.Telemetry;
using Domain.Tenants.Entities;

namespace Domain.Devices.Telemetry;

public partial class KioskHeartbeat : AppendOnlySyncEntity
{
    public Guid KioskId { get; set; }

    public Guid NodeId { get; set; }

    public long? HeartbeatSequence { get; set; }

    public DateTimeOffset ReportedAt { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }

    public KioskHeartbeatStatus Status { get; set; } = KioskHeartbeatStatus.Online;

    public string? RobotStatus { get; set; }

    public string? NetworkStatus { get; set; }

    public string? AppVersion { get; set; }

    public string? FirmwareVersion { get; set; }

    public decimal? CpuUsagePercent { get; set; }

    public decimal? MemoryUsagePercent { get; set; }

    public decimal? DiskUsagePercent { get; set; }

    public int PendingSyncEventCount { get; set; }

    public string? PayloadJson { get; set; }

    public virtual Kiosk Kiosk { get; set; } = null!;
}
