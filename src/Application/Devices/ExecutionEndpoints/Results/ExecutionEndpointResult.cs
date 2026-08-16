namespace Application.Devices.ExecutionEndpoints.Results;

public sealed class ExecutionEndpointResult
{
    public Guid Id { get; init; }
    public Guid KioskId { get; init; }
    public string KioskCode { get; init; } = null!;
    public string EndpointCode { get; init; } = null!;
    public string ExecutionProfile { get; init; } = null!;
    public string AuthenticationMode { get; init; } = null!;
    public string Status { get; init; } = null!;
    public Guid? ProfileIdentity { get; init; }
    public string? CredentialStatus { get; init; }
    public string? MqttUsername { get; init; }
    public string? MqttCredentialStatus { get; init; }
    public int? MqttCredentialVersion { get; init; }
    public ExecutionEndpointReadinessResult? Readiness { get; init; }
    public DateTimeOffset? ProvisionedAt { get; init; }
    public Guid? ReportedDevicesSourceExecutorId { get; init; }
    public long? ReportedDevicesSnapshotRevision { get; init; }
    public DateTimeOffset? ReportedDevicesObservedAt { get; init; }
    public DateTimeOffset? ReportedDevicesReceivedAt { get; init; }
    public IReadOnlyList<ExecutionEndpointReportedDeviceResult> ReportedDevices { get; init; } = [];
}

public sealed class ExecutionEndpointReadinessResult
{
    public long StateRevision { get; init; }
    public required string Readiness { get; init; }
    public required string Activity { get; init; }
    public required string Safety { get; init; }
    public Guid? CurrentCommandId { get; init; }
    public required string PhysicalOutputState { get; init; }
    public string? FaultCode { get; init; }
    public DateTimeOffset ExecutorReportedAt { get; init; }
    public IReadOnlyList<ExecutionEndpointCapabilityResult> Capabilities { get; init; } = [];
}
public sealed class ExecutionEndpointCapabilityResult
{
    public required string CapabilityCode { get; init; }
    public string? WorkcellCode { get; init; }
    public bool IsAvailable { get; init; }
    public string? UnavailableReason { get; init; }
}

public sealed class ExecutionEndpointReportedDeviceResult
{
    public Guid Id { get; init; }
    public string SourceDeviceKey { get; init; } = null!;
    public Guid? DeviceId { get; init; }
    public string RuntimeTargetCode { get; init; } = null!;
    public string MachineModelCode { get; init; } = null!;
}
