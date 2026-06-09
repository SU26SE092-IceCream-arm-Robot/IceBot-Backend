using Application.Identity.Tokens.Claims;

namespace Application.Tenants.RoleScopes.Queries;

public sealed class GetRoleScopeOptionsQuery
{
    public string RoleCode { get; init; } = null!;
    public CurrentUserContext UserContext { get; init; } = null!;
    public IReadOnlyCollection<string> UserRoles { get; init; } = Array.Empty<string>();
}
