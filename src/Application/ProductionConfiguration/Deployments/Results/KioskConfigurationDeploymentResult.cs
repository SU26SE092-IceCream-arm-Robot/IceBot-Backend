using Domain.ProductionConfiguration.Entities;

using System.Text.Json.Serialization;

namespace Application.ProductionConfiguration.Deployments.Results;

public sealed class KioskConfigurationDeploymentResult
{
    public Guid Id { get; init; }
    public Guid KioskId { get; init; }
    public Guid KioskExecutionEndpointId { get; init; }
    public Guid ConfigurationReleaseId { get; init; }
    public string ReleaseChecksum { get; init; } = null!;
    public int AttemptNo { get; init; }
    public string Status { get; init; } = null!;
    public DateTimeOffset RequestedAt { get; init; }
    public Guid? RequestedByAccountId { get; init; }
    public string ValidationReportChecksum { get; init; } = null!;
    public string RiskLevel { get; init; } = null!;
    public DateTimeOffset? RiskAcknowledgedAt { get; init; }
    [JsonIgnore]
    public Guid? EdgeCommandId { get; init; }

    public static KioskConfigurationDeploymentResult FromEntity(
        KioskConfigurationDeployment deployment,
        Guid? edgeCommandId = null)
    {
        return new KioskConfigurationDeploymentResult
        {
            Id = deployment.Id,
            KioskId = deployment.KioskId,
            KioskExecutionEndpointId = deployment.KioskExecutionEndpointId,
            ConfigurationReleaseId = deployment.ConfigurationReleaseId,
            ReleaseChecksum = deployment.ReleaseChecksum,
            AttemptNo = deployment.AttemptNo,
            Status = deployment.Status.ToString(),
            RequestedAt = deployment.RequestedAt,
            RequestedByAccountId = deployment.RequestedByAccountId,
            ValidationReportChecksum = deployment.ValidationReportChecksum,
            RiskLevel = deployment.RiskLevel,
            RiskAcknowledgedAt = deployment.RiskAcknowledgedAt,
            EdgeCommandId = edgeCommandId
        };
    }
}
