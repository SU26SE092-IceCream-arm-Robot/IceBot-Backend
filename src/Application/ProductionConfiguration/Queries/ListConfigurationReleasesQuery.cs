using Application.Identity.Tokens.Claims;
using Domain.ProductionConfiguration.Enums;

namespace Application.ProductionConfiguration.Queries;

public sealed class ListConfigurationReleasesQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public Guid? OrganizationId { get; init; }
    public ConfigurationReleaseStatus? Status { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
