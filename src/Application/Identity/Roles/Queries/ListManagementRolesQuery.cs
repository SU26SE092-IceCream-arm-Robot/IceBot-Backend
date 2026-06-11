using Application.Identity.Tokens.Claims;

namespace Application.Identity.Roles.Queries;

public sealed class ListManagementRolesQuery
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public IReadOnlyCollection<string> UserRoles { get; init; } = null!;
}
