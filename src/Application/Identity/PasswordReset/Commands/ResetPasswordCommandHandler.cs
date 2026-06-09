using Application.Identity.Abstractions;
using Application.Identity.Tokens.Services;
using Application.Shared.Wrappers;
using Domain.Identity.Enums;
using Domain.Identity.ValueObjects;

namespace Application.Identity.PasswordReset.Commands;

public sealed class ResetPasswordCommandHandler
{
    private readonly IPasswordResetRequestStore _passwordResetRequests;
    private readonly IPasswordHasher _passwordHasher;
    private readonly RefreshTokenService _refreshTokens;

    public ResetPasswordCommandHandler(
        IPasswordResetRequestStore passwordResetRequests,
        IPasswordHasher passwordHasher,
        RefreshTokenService refreshTokens)
    {
        _passwordResetRequests = passwordResetRequests;
        _passwordHasher = passwordHasher;
        _refreshTokens = refreshTokens;
    }

    public async Task<ApiResult<bool>> HandleAsync(
        ResetPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var request = command.Request;
        var ipAddress = command.IpAddress;
        var userAgent = command.UserAgent;

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return ApiResult<bool>.Fail("Password reset token is required.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return ApiResult<bool>.Fail("New password is required.");
        }

        var tokenHash = PasswordResetTokenHelper.HashToken(request.Token.Trim());
        var passwordResetRequest = await _passwordResetRequests.GetByTokenHashAsync(tokenHash, asNoTracking: false, cancellationToken);
        if (passwordResetRequest is null ||
            passwordResetRequest.UsedAt is not null ||
            passwordResetRequest.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return ApiResult<bool>.Fail("Password reset token is invalid or expired.", 400);
        }

        var account = passwordResetRequest.Account;
        if (account.Status != AccountStatus.Active)
        {
            return ApiResult<bool>.Fail("Account is not active.", 403);
        }

        if (!account.LocalLoginEnabled)
        {
            return ApiResult<bool>.Fail("Local password login is not enabled for this account.", 403);
        }

        account.Password = HashedPassword.From(_passwordHasher.HashPassword(request.NewPassword));
        account.FailedLoginCount = 0;
        account.LockedUntil = null;
        account.UpdatedAt = DateTimeOffset.UtcNow;
        account.UpdatedByAccountId = account.Id;
        passwordResetRequest.UsedAt = DateTimeOffset.UtcNow;
        passwordResetRequest.UsedByIp = ipAddress;
        passwordResetRequest.UsedByUserAgent = userAgent;

        await _passwordResetRequests.SaveChangesAsync(cancellationToken);
        await _refreshTokens.RevokeAllForAccountAsync(account.Id, "Password reset completed", ipAddress, userAgent);

        return ApiResult<bool>.Success(true, "Password has been reset.");
    }
}
