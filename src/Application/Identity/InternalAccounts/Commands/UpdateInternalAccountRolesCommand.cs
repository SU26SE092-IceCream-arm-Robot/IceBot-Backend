using Application.Identity.InternalAccounts.Requests;
using Application.Identity.Tokens.Claims;

namespace Application.Identity.InternalAccounts.Commands;

public sealed class UpdateInternalAccountRolesCommand
{
    public required Guid AccountId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required UpdateAccountRolesRequest Request { get; init; }
    public Guid? UpdatedByAccountId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
    public required IReadOnlyCollection<string> UserRoles { get; init; }
}
