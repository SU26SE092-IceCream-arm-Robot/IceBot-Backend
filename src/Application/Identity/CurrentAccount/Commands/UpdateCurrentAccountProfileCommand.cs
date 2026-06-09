using Application.Identity.CurrentAccount.Requests;

namespace Application.Identity.CurrentAccount.Commands;

public sealed class UpdateCurrentAccountProfileCommand
{
    public Guid AccountId { get; init; }
    public UpdateCurrentAccountProfileRequest Request { get; init; } = null!;
}
