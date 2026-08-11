using Application.Identity.Tokens.Services;
using Application.Shared.Wrappers;

namespace Application.Identity.CurrentAccount.Commands;

public sealed class RevokeCurrentAccountSessionCommandHandler(RefreshTokenService refreshTokens)
{
    public async Task<ApiResult<object>> HandleAsync(
        RevokeCurrentAccountSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.AccountId == Guid.Empty || command.SessionId == Guid.Empty)
        {
            return ApiResult<object>.Fail("A valid account and session id are required.");
        }

        await refreshTokens.RevokeSessionForAccountAsync(
            command.AccountId,
            command.SessionId,
            "Revoked by account owner",
            command.IpAddress,
            command.UserAgent);
        return ApiResult<object>.Success(new { }, "Session revoked.", 204);
    }
}
