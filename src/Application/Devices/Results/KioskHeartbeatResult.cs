using Domain.Devices.Enums;

namespace Application.Devices.Results;

public sealed class KioskHeartbeatResult
{
    public Guid Id { get; set; }
    public Guid KioskId { get; set; }
    public Guid NodeId { get; set; }
    public Guid OriginNodeId { get; set; }
    public long? HeartbeatSequence { get; set; }
    public DateTimeOffset ReportedAt { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public KioskHeartbeatStatus Status { get; set; }
    public string? RobotStatus { get; set; }
    public string? NetworkStatus { get; set; }
    public string? AppVersion { get; set; }
    public string? FirmwareVersion { get; set; }
    public decimal? CpuUsagePercent { get; set; }
    public decimal? MemoryUsagePercent { get; set; }
    public decimal? DiskUsagePercent { get; set; }
    public int PendingSyncEventCount { get; set; }
}
