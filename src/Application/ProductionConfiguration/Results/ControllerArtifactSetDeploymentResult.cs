using Domain.ProductionConfiguration.Entities;

namespace Application.ProductionConfiguration.Results;

public sealed class ControllerArtifactSetDeploymentResult
{
    public Guid Id { get; init; }
    public Guid KioskId { get; init; }
    public Guid KioskExecutionEndpointId { get; init; }
    public Guid SourceConfigurationReleaseId { get; init; }
    public string ReleaseChecksum { get; init; } = null!;
    public long ActiveSetVersion { get; init; }
    public string ActiveSetChecksum { get; init; } = null!;
    public int RequestedArtifactCount { get; init; }
    public long RequestedArtifactStorageBytes { get; init; }
    public string Status { get; init; } = null!;
    public DateTimeOffset RequestedAt { get; init; }
    public Guid? RequestedByAccountId { get; init; }
    public Guid? EdgeCommandId { get; init; }

    public static ControllerArtifactSetDeploymentResult FromEntity(
        ControllerArtifactSetDeployment deployment,
        Guid? edgeCommandId = null)
    {
        return new ControllerArtifactSetDeploymentResult
        {
            Id = deployment.Id,
            KioskId = deployment.KioskId,
            KioskExecutionEndpointId = deployment.KioskExecutionEndpointId,
            SourceConfigurationReleaseId = deployment.SourceConfigurationReleaseId,
            ReleaseChecksum = deployment.ReleaseChecksum,
            ActiveSetVersion = deployment.ActiveSetVersion,
            ActiveSetChecksum = deployment.ActiveSetChecksum,
            RequestedArtifactCount = deployment.RequestedArtifactCount,
            RequestedArtifactStorageBytes = deployment.RequestedArtifactStorageBytes,
            Status = deployment.Status.ToString(),
            RequestedAt = deployment.RequestedAt,
            RequestedByAccountId = deployment.RequestedByAccountId,
            EdgeCommandId = edgeCommandId
        };
    }
}
