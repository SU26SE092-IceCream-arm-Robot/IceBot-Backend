using Application.Identity.Authentication.Requests;

namespace Application.Identity.Authentication.Commands;

public sealed class GoogleLoginCommand
{
    public ExternalLoginRequest Request { get; init; } = null!;
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}
