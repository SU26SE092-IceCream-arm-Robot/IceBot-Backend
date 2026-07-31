using Application.Identity.Tokens.Claims;

namespace Application.Identity.InternalAccounts.Commands;

public sealed class DisableInternalAccountCommand
{
    public Guid AccountId { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid? UpdatedByAccountId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
}
