using Application.Identity.Tokens.Services;
using Application.Shared.Wrappers;

namespace Application.Identity.Authentication.Commands;

public sealed class RevokeCurrentAccountTokensCommandHandler
{
    private readonly AccountTokenService _tokenService;

    public RevokeCurrentAccountTokensCommandHandler(AccountTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task<ApiResult<object>> HandleAsync(
        RevokeCurrentAccountTokensCommand command,
        CancellationToken cancellationToken = default)
    {
        var revokedCount = await _tokenService.RevokeAllForAccountAsync(
            command.AccountId,
            command.Request?.Reason,
            command.IpAddress,
            command.UserAgent);

        return ApiResult<object>.Success(
            new { revoked = revokedCount },
            "All sessions revoked",
            200);
    }
}
