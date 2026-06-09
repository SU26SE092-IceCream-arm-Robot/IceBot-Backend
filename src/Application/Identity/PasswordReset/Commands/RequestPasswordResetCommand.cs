using Application.Identity.PasswordReset.Requests;

namespace Application.Identity.PasswordReset.Commands;

public sealed class RequestPasswordResetCommand
{
    public RequestPasswordResetRequest Request { get; init; } = null!;
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}
