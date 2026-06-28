using Application.Identity.Tokens.Claims;

namespace Application.ProductionConfiguration.Queries;

public sealed record GetConfigurationDeploymentQuery(Guid DeploymentId)
{
    public required CurrentUserContext UserContext { get; init; }
}
