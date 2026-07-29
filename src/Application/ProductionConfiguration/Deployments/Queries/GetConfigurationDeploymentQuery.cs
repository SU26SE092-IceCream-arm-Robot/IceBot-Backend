using Application.Identity.Tokens.Claims;

namespace Application.ProductionConfiguration.Deployments.Queries;

public sealed record GetConfigurationDeploymentQuery(Guid KioskId, Guid DeploymentId)
{
    public required CurrentUserContext UserContext { get; init; }
}
