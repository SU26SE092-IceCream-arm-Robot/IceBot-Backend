namespace Application.Devices.Operations.Results;

public sealed class KioskOperationsWorkspaceResult
{
    public required KioskOperationsWorkspaceKioskResult Kiosk { get; init; }
    public required KioskOperationsWorkspaceConnectivityResult Connectivity { get; init; }
    public required KioskOperationsWorkspaceExecutionResult Execution { get; init; }
    public required KioskOperationsWorkspaceConfigurationResult Configuration { get; init; }
    public required KioskOperationsWorkspaceActionsResult AvailableActions { get; init; }
}

public sealed class KioskOperationsWorkspaceKioskResult
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid StoreId { get; init; }
    public string StoreName { get; init; } = null!;
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string LifecycleStatus { get; init; } = null!;
    public string OperationalState { get; init; } = null!;
    public string? OperationalStateReason { get; init; }
    public DateTimeOffset? OperationalStateChangedAt { get; init; }
}

public sealed class KioskOperationsWorkspaceConnectivityResult
{
    public string Status { get; init; } = null!;
    public DateTimeOffset? LastHeartbeatAt { get; init; }
    public string? LatestHeartbeatStatus { get; init; }
    public DateTimeOffset? LatestHeartbeatReportedAt { get; init; }
    public string? LatestEventType { get; init; }
    public string? LatestEventSeverity { get; init; }
    public DateTimeOffset? LatestEventAt { get; init; }
}

public sealed class KioskOperationsWorkspaceExecutionResult
{
    public int EndpointCount { get; init; }
    public int ActiveEndpointCount { get; init; }
    public int ReadyEndpointCount { get; init; }
    public bool HasMultipleReadyEndpoints { get; init; }
    public KioskOperationsWorkspaceEndpointResult? SoleReadyEndpoint { get; init; }
}

public sealed class KioskOperationsWorkspaceEndpointResult
{
    public Guid EndpointId { get; init; }
    public string EndpointCode { get; init; } = null!;
    public string ExecutionProfile { get; init; } = null!;
    public string? Readiness { get; init; }
    public string? Activity { get; init; }
    public string? Safety { get; init; }
    public string? FaultCode { get; init; }
    public DateTimeOffset? ReportedAt { get; init; }
}

public sealed class KioskOperationsWorkspaceConfigurationResult
{
    public Guid? ActiveReleaseId { get; init; }
    public Guid? ActiveDeploymentId { get; init; }
    public DateTimeOffset? ActiveConfigurationReportedAt { get; init; }
}

public sealed class KioskOperationsWorkspaceActionsResult
{
    public bool CanManageKiosk { get; init; }
    public bool CanViewDeployment { get; init; }
}
