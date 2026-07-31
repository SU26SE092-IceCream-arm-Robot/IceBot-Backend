using Application.Identity.Tokens.Claims;

namespace Application.Identity.InternalAccounts.Commands;

public sealed class CreateInternalAccountInvitationCommand
{
    public Guid AccountId { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid? InvitedByAccountId { get; init; }
    public bool SendEmail { get; init; }
    public required CurrentUserContext UserContext { get; init; }
}
