using Application.Identity.Tokens.Claims;

namespace Application.Tenants.Organizations.Queries;

public sealed class GetOrganizationQuery
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public Guid OrganizationId { get; init; }
}
