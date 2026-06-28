using Application.Identity.Tokens.Claims;

namespace Application.ProductionConfiguration.Commands;

public sealed class DeployFullEdgeConfigurationCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public required Guid KioskId { get; init; }
    public required Guid ConfigurationReleaseId { get; init; }
    public required Guid KioskExecutionEndpointId { get; init; }
    public required string IdempotencyKey { get; init; }
    public DateTimeOffset? CommandExpiryAt { get; init; }
    public Guid? RollbackTargetDeploymentId { get; init; }
    public bool IsRollback => RollbackTargetDeploymentId.HasValue;
}
