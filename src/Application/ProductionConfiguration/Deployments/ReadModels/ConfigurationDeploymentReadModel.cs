namespace Application.ProductionConfiguration.Deployments.ReadModels;

public enum ConfigurationDeploymentProfile
{
    FullEdge = 1,
    LowCostController = 2
}

public enum ConfigurationDeploymentReadStatus
{
    Pending = 1,
    Installed = 2,
    Active = 3,
    Failed = 4
}

public sealed class ConfigurationDeploymentReadModel
{
    public Guid Id { get; init; }
    public ConfigurationDeploymentProfile Profile { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid StoreId { get; init; }
    public Guid KioskId { get; init; }
    public Guid KioskExecutionEndpointId { get; init; }
    public string EndpointCode { get; init; } = string.Empty;
    public Guid? ObservedActiveDeploymentId { get; init; }
    public Guid ConfigurationReleaseId { get; init; }
    public long ReleaseNumber { get; init; }
    public string ReleaseChecksum { get; init; } = string.Empty;
    public ConfigurationDeploymentReadStatus Status { get; init; }
    public DateTimeOffset RequestedAt { get; init; }
    public Guid? RequestedByAccountId { get; init; }
    public DateTimeOffset? ExecutorReportedAt { get; init; }
    public DateTimeOffset? CloudReceivedAt { get; init; }
    public Guid? LastReportId { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public int? AttemptNo { get; init; }
    public Guid? EdgeRuntimeId { get; init; }
    public Guid? ControllerId { get; init; }
    public long? ActiveSetVersion { get; init; }
    public string? ActiveSetChecksum { get; init; }
    public int? RequestedArtifactCount { get; init; }
    public long? RequestedArtifactStorageBytes { get; init; }
    public int? MaxArtifactCount { get; init; }
    public long? MaxArtifactStorageBytes { get; init; }
}
