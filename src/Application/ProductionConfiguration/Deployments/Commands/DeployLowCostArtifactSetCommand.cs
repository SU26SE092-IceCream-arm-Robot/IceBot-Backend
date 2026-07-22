using Application.Identity.Tokens.Claims;

namespace Application.ProductionConfiguration.Deployments.Commands;

public sealed class DeployLowCostArtifactSetCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid KioskId { get; init; }
    public required Guid ConfigurationReleaseId { get; init; }
    public required Guid KioskExecutionEndpointId { get; init; }
    public required string IdempotencyKey { get; init; }
    public required IReadOnlyCollection<DeployLowCostArtifactSelection> Selections { get; init; }
    public DateTimeOffset? CommandExpiryAt { get; init; }
    public Guid? RollbackTargetDeploymentId { get; init; }
    public string DeploymentPreviewChecksum { get; init; } = string.Empty;
    public bool AcknowledgeRemainingRisk { get; init; }
    public bool IsRollback => RollbackTargetDeploymentId.HasValue;
}

public sealed record DeployLowCostArtifactSelection(
    Guid ExecutionRouteId,
    Guid RobotProgramId);
