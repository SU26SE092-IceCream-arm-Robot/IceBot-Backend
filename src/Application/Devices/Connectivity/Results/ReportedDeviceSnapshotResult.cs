namespace Application.Devices.Connectivity.Results;

public sealed class ReportedDeviceSnapshotResult
{
    public Guid EndpointId { get; init; }
    public long SnapshotRevision { get; init; }
    public bool Applied { get; init; }
    public bool DuplicateOrStale { get; init; }
    public DateTimeOffset? CloudReceivedAt { get; init; }
    public IReadOnlyList<ReportedDeviceResult> Devices { get; init; } = [];
}

public sealed class ReportedDeviceResult
{
    public string SourceDeviceKey { get; init; } = null!;
    public Guid? DeviceId { get; init; }
    public string RuntimeTargetCode { get; init; } = null!;
    public string MachineModelCode { get; init; } = null!;
}
