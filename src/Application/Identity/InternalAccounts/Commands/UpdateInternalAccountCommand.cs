using Application.Identity.InternalAccounts.Requests;

namespace Application.Identity.InternalAccounts.Commands;

public sealed class UpdateInternalAccountCommand
{
    public Guid AccountId { get; init; }
    public UpdateInternalAccountRequest Request { get; init; } = null!;
    public Guid? UpdatedByAccountId { get; init; }
}
