using Application.Identity.Tokens.Services;
using Application.Shared.Wrappers;

namespace Application.Identity.Authentication.Commands;

public sealed class RevokeRefreshTokenCommandHandler
{
    private readonly AccountTokenService _tokenService;

    public RevokeRefreshTokenCommandHandler(AccountTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task<ApiResult<object>> HandleAsync(
        RevokeRefreshTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        var request = command.Request;

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return ApiResult<object>.Fail("Refresh token is required.", 400);
        }

        var revoked = await _tokenService.RevokeByTokenAsync(
            request.RefreshToken,
            request.Reason,
            command.IpAddress,
            command.UserAgent);

        return ApiResult<object>.Success(
            new { revoked },
            revoked ? "Revoked" : "Not found",
            revoked ? 200 : 404);
    }
}
