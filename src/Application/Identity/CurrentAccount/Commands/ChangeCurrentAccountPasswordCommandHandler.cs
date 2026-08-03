using Application.Identity.Abstractions;
using Application.Identity.Tokens.Services;
using Application.Shared.Wrappers;
using Domain.Identity.Enums;
using Domain.Identity.ValueObjects;

namespace Application.Identity.CurrentAccount.Commands;

public sealed class ChangeCurrentAccountPasswordCommandHandler
{
    private readonly IIdentityAccountStore _accounts;
    private readonly IPasswordHasher _passwordHasher;
    private readonly RefreshTokenService _refreshTokens;

    public ChangeCurrentAccountPasswordCommandHandler(
        IIdentityAccountStore accounts,
        IPasswordHasher passwordHasher,
        RefreshTokenService refreshTokens)
    {
        _accounts = accounts;
        _passwordHasher = passwordHasher;
        _refreshTokens = refreshTokens;
    }

    public async Task<ApiResult<bool>> HandleAsync(
        ChangeCurrentAccountPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var accountId = command.AccountId;
        var request = command.Request;
        var ipAddress = command.IpAddress;
        var userAgent = command.UserAgent;

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return ApiResult<bool>.Fail("Current password is required.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return ApiResult<bool>.Fail("New password is required.");
        }

        return await _accounts.ExecuteInTransactionAsync(async () =>
        {
            var account = await _accounts.GetByIdAsync(accountId, asNoTracking: false, cancellationToken: cancellationToken);
            if (account is null)
            {
                return ApiResult<bool>.Fail("Account not found.", 404);
            }

            if (account.Status != AccountStatus.Active)
            {
                return ApiResult<bool>.Fail("Account is not active.", 403);
            }

            if (!account.LocalLoginEnabled || account.Password is null)
            {
                return ApiResult<bool>.Fail("Local password login is not enabled for this account.", 403);
            }

            if (!_passwordHasher.VerifyPassword(request.CurrentPassword, account.Password.Value))
            {
                return ApiResult<bool>.Fail("Current password is incorrect.", 400);
            }

            account.Password = HashedPassword.From(_passwordHasher.HashPassword(request.NewPassword));
            account.FailedLoginCount = 0;
            account.LockedUntil = null;
            account.UpdatedAt = DateTimeOffset.UtcNow;
            account.UpdatedByAccountId = account.Id;

            await _refreshTokens.RevokeAllForAccountAsync(
                account.Id,
                "Password changed by account owner",
                ipAddress,
                userAgent);

            return ApiResult<bool>.Success(true, "Password changed.");
        }, cancellationToken);
    }
}
