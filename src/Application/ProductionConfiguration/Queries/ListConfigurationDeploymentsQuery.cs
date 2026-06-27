using Application.Identity.Tokens.Claims;
using Application.ProductionConfiguration.ReadModels;

namespace Application.ProductionConfiguration.Queries;

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
