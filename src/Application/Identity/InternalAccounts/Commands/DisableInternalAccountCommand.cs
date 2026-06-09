namespace Application.Identity.InternalAccounts.Commands;

public sealed class DisableInternalAccountCommand
{
    public Guid AccountId { get; init; }
    public Guid? UpdatedByAccountId { get; init; }
}
