using Application.Identity.CurrentAccount.Requests;

namespace Application.Identity.CurrentAccount.Commands;

public sealed class ChangeCurrentAccountPasswordCommand
{
    public Guid AccountId { get; init; }
    public ChangeCurrentAccountPasswordRequest Request { get; init; } = null!;
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}
