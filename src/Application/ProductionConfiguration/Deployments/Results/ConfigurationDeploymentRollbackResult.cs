using System.Text.Json.Serialization;

namespace Application.ProductionConfiguration.Deployments.Results;

public sealed class ConfigurationDeploymentRollbackResult
{
    public Guid TargetDeploymentId { get; init; }
    public Guid NewDeploymentId { get; init; }
    [JsonIgnore]
    public Guid EdgeCommandId { get; init; }
    public string Profile { get; init; } = string.Empty;
    public Guid KioskId { get; init; }
    public Guid KioskExecutionEndpointId { get; init; }
    public Guid ConfigurationReleaseId { get; init; }
    public string ReleaseChecksum { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}
