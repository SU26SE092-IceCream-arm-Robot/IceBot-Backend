using Application.Identity.Authentication.Requests;

namespace Application.Identity.Authentication.Commands;

public sealed class RevokeCurrentAccountTokensCommand
{
    public Guid AccountId { get; init; }
    public RevokeAccountTokensRequest? Request { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}
