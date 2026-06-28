using Application.Identity.Tokens.Claims;

namespace Application.ProductionConfiguration.Commands;

public sealed class RollbackConfigurationDeploymentCommand
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid TargetDeploymentId { get; init; }
    public required string IdempotencyKey { get; init; }
    public DateTimeOffset? CommandExpiryAt { get; init; }
}
