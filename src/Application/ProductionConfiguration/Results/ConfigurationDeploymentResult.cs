using Application.ProductionConfiguration.ReadModels;

namespace Application.ProductionConfiguration.Results;

public sealed class ConfigurationDeploymentResult
{
    public Guid Id { get; init; }
    public string Profile { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public Guid StoreId { get; init; }
    public Guid KioskId { get; init; }
    public Guid KioskExecutionEndpointId { get; init; }
    public string EndpointCode { get; init; } = string.Empty;
    public Guid ConfigurationReleaseId { get; init; }
    public long ReleaseNumber { get; init; }
    public string ReleaseChecksum { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
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

    public static ConfigurationDeploymentResult FromReadModel(ConfigurationDeploymentReadModel deployment)
    {
        return new ConfigurationDeploymentResult
        {
            Id = deployment.Id,
            Profile = deployment.Profile.ToString(),
            OrganizationId = deployment.OrganizationId,
            StoreId = deployment.StoreId,
            KioskId = deployment.KioskId,
            KioskExecutionEndpointId = deployment.KioskExecutionEndpointId,
            EndpointCode = deployment.EndpointCode,
            ConfigurationReleaseId = deployment.ConfigurationReleaseId,
            ReleaseNumber = deployment.ReleaseNumber,
            ReleaseChecksum = deployment.ReleaseChecksum,
            Status = deployment.Status.ToString(),
            RequestedAt = deployment.RequestedAt,
            RequestedByAccountId = deployment.RequestedByAccountId,
            ExecutorReportedAt = deployment.ExecutorReportedAt,
            CloudReceivedAt = deployment.CloudReceivedAt,
            LastReportId = deployment.LastReportId,
            FailureCode = deployment.FailureCode,
            FailureReason = deployment.FailureReason,
            AttemptNo = deployment.AttemptNo,
            EdgeRuntimeId = deployment.EdgeRuntimeId,
            ControllerId = deployment.ControllerId,
            ActiveSetVersion = deployment.ActiveSetVersion,
            ActiveSetChecksum = deployment.ActiveSetChecksum,
            RequestedArtifactCount = deployment.RequestedArtifactCount,
            RequestedArtifactStorageBytes = deployment.RequestedArtifactStorageBytes,
            MaxArtifactCount = deployment.MaxArtifactCount,
            MaxArtifactStorageBytes = deployment.MaxArtifactStorageBytes
        };
    }
}
