using Application.Identity.InternalAccounts.Requests;

namespace Application.Identity.InternalAccounts.Commands;

public sealed class SetInternalAccountPasswordCommand
{
    public Guid AccountId { get; init; }
    public SetInternalAccountPasswordRequest Request { get; init; } = null!;
    public Guid? UpdatedByAccountId { get; init; }
}
