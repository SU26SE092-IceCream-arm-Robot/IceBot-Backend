using Application.Identity.Tokens.Claims;

namespace Application.ProductionConfiguration.Releases.Queries;

public sealed record GetConfigurationReleaseQuery(Guid OrganizationId, Guid ReleaseId)
{
    public required CurrentUserContext UserContext { get; init; }
}
