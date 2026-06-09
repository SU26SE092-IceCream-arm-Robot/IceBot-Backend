using Application.Identity.Tokens.Claims;

namespace Application.Tenants.TenantTree.Queries;

public sealed class GetTenantTreeQuery
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public bool IncludeInactive { get; init; }
}
