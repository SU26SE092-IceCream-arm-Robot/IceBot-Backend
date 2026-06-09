using Application.Identity.PasswordReset.Requests;

namespace Application.Identity.PasswordReset.Commands;

public sealed class ResetPasswordCommand
{
    public ResetPasswordRequest Request { get; init; } = null!;
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}
