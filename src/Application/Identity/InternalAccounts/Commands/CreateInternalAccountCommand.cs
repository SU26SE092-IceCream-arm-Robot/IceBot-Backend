using Application.Identity.InternalAccounts.Requests;

namespace Application.Identity.InternalAccounts.Commands;

public sealed class CreateInternalAccountCommand
{
    public CreateInternalAccountRequest Request { get; init; } = null!;
    public Guid? CreatedByAccountId { get; init; }
}
