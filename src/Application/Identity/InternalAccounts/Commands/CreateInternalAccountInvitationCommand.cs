namespace Application.Identity.InternalAccounts.Commands;

public sealed class CreateInternalAccountInvitationCommand
{
    public Guid AccountId { get; init; }
    public Guid? InvitedByAccountId { get; init; }
    public bool SendEmail { get; init; }
}
