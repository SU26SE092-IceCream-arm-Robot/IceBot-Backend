namespace Application.Devices.Results;

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
    public Guid? CredentialBindingId { get; init; }
    public string? CredentialStatus { get; init; }
    public DateTimeOffset? ProvisionedAt { get; init; }
    public IReadOnlyList<ExecutionEndpointRobotTargetResult> SupportedRobotTargets { get; init; } = [];
}

public sealed class ExecutionEndpointRobotTargetResult
{
    public Guid Id { get; init; }
    public string RuntimeTargetCode { get; init; } = null!;
    public string MachineModelCode { get; init; } = null!;
    public Guid? DeviceId { get; init; }
    public string? DeviceCode { get; init; }
    public string? DeviceName { get; init; }
}
