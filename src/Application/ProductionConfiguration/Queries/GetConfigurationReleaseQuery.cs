using Application.Identity.Tokens.Claims;

namespace Application.ProductionConfiguration.Queries;

public sealed record GetConfigurationReleaseQuery(Guid ReleaseId)
{
    public required CurrentUserContext UserContext { get; init; }
}
