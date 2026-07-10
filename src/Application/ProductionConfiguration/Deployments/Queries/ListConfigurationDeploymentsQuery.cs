using Application.Identity.Tokens.Claims;
using Application.ProductionConfiguration.Releases.ReadModels;
using Application.ProductionConfiguration.Deployments.ReadModels;

namespace Application.ProductionConfiguration.Deployments.Queries;

public sealed class ListConfigurationDeploymentsQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public Guid? ConfigurationReleaseId { get; init; }
    public ConfigurationDeploymentProfile? Profile { get; init; }
    public ConfigurationDeploymentReadStatus? Status { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
