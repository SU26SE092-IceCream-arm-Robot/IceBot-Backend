using Domain.Devices.Telemetry;
using Domain.Devices.Catalog;

namespace Application.Devices.Telemetry.Commands;

public sealed class IngestKioskHeartbeatCommand
{
    public required Guid KioskId { get; init; }
    public required Guid EndpointId { get; init; }
    public required Guid OriginNodeId { get; init; }
    public required long HeartbeatSequence { get; init; }
    public required DateTimeOffset ReportedAt { get; init; }
    public KioskHeartbeatStatus Status { get; init; } = KioskHeartbeatStatus.Online;
    public string? RobotStatus { get; init; }
    public string? NetworkStatus { get; init; }
    public string? AppVersion { get; init; }
    public string? FirmwareVersion { get; init; }
    public decimal? CpuUsagePercent { get; init; }
    public decimal? MemoryUsagePercent { get; init; }
    public decimal? DiskUsagePercent { get; init; }
    public int PendingSyncEventCount { get; init; }
}
