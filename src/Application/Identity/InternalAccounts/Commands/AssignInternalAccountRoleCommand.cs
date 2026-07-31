using Application.Identity.InternalAccounts.Requests;
using Application.Identity.Tokens.Claims;

namespace Application.Identity.InternalAccounts.Commands;

public sealed class AssignInternalAccountRoleCommand
{
    public Guid AccountId { get; init; }
    public Guid OrganizationId { get; init; }
    public AccountRoleScopeRequest Request { get; init; } = null!;
    public Guid? AssignedByAccountId { get; init; }
    public CurrentUserContext UserContext { get; init; } = new();
    public IReadOnlyCollection<string> UserRoles { get; init; } = Array.Empty<string>();
}
