using Domain.Devices.Telemetry;
using Domain.Common.Enums;
using Domain.Devices.Enums;

namespace Application.Devices.Commands;

public enum BatchSyncEventType
{
    Heartbeat = 1,
    DeviceEvent = 2,
    LocalLog = 3
}

public sealed class IngestBatchEventsCommand
{
    public required Guid KioskId { get; init; }
    public required Guid EndpointId { get; init; }
    public required Guid OriginNodeId { get; init; }
    public required IReadOnlyList<BatchSyncEventItem> Events { get; init; }
}

public sealed class BatchSyncEventItem
{
    public required Guid EventId { get; init; }
    public required BatchSyncEventType EventType { get; init; }
    public BatchHeartbeatData? Heartbeat { get; init; }
    public BatchDeviceEventData? DeviceEvent { get; init; }
    public BatchLocalLogData? LocalLog { get; init; }
}

public sealed class BatchHeartbeatData
{
    public long HeartbeatSequence { get; init; }
    public DateTimeOffset ReportedAt { get; init; }
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

public sealed class BatchDeviceEventData
{
    public Guid DeviceId { get; init; }
    public Guid? CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public required string EventType { get; init; }
    public SeverityLevel Severity { get; init; }
    public required string Message { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public string? PayloadJson { get; init; }
}

public sealed class BatchLocalLogData
{
    public Guid? DeviceId { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public required string Action { get; init; }
    public string Category { get; init; } = "System";
    public SeverityLevel Severity { get; init; } = SeverityLevel.Info;
    public required string Message { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public string? PayloadJson { get; init; }
}
