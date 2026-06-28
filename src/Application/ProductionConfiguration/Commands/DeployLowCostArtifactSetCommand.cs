using Application.Identity.Tokens.Claims;

namespace Application.ProductionConfiguration.Commands;

public sealed class DeployLowCostArtifactSetCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid KioskId { get; init; }
    public required Guid ConfigurationReleaseId { get; init; }
    public required Guid KioskExecutionEndpointId { get; init; }
    public required int MaxArtifactCount { get; init; }
    public required long MaxArtifactStorageBytes { get; init; }
    public required IReadOnlyCollection<DeployLowCostArtifactSelection> Selections { get; init; }
    public DateTimeOffset? CommandExpiryAt { get; init; }
    public Guid? RollbackTargetDeploymentId { get; init; }
    public bool IsRollback => RollbackTargetDeploymentId.HasValue;
}

public sealed record DeployLowCostArtifactSelection(
    Guid ExecutionRouteId,
    Guid RobotProgramId,
    Guid RobotArtifactId,
    int RunOrder);
