using Application.Email;
using Application.Identity.Abstractions;
using Application.Identity.PasswordReset.Requests;
using Application.Identity.Tokens.Services;
using Application.Shared.Wrappers;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Identity.ValueObjects;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Application.Identity.PasswordReset.Services;

public sealed class PasswordResetService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);

    private readonly IIdentityAccountStore _accounts;
    private readonly IPasswordResetRequestStore _passwordResetRequests;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly IOptions<EmailOptions> _emailOptions;
    private readonly RefreshTokenService _refreshTokens;

    public PasswordResetService(
        IIdentityAccountStore accounts,
        IPasswordResetRequestStore passwordResetRequests,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        IOptions<EmailOptions> emailOptions,
        RefreshTokenService refreshTokens)
    {
        _accounts = accounts;
        _passwordResetRequests = passwordResetRequests;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _emailOptions = emailOptions;
        _refreshTokens = refreshTokens;
    }

    public async Task<ApiResult<bool>> RequestResetAsync(
        RequestPasswordResetRequest request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.EmailOrUserName))
        {
            return ApiResult<bool>.Fail("Email or username is required.");
        }

        var login = NormalizeEmailOrUserName(request.EmailOrUserName);
        var account = await _accounts.GetByEmailOrUserNameAsync(login, asNoTracking: false, cancellationToken);

        // Avoid account enumeration. Always return success for unknown or ineligible accounts.
        if (account is null ||
            account.Status != AccountStatus.Active ||
            !account.LocalLoginEnabled ||
            string.IsNullOrWhiteSpace(account.Email))
        {
            return ApiResult<bool>.Success(true, "If the account exists, password reset instructions have been sent.");
        }

        var rawToken = CreateToken();
        var passwordResetRequest = new PasswordResetRequest
        {
            AccountId = account.Id,
            TokenHash = HashToken(rawToken),
            RequestedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(TokenLifetime),
            RequestedByIp = ipAddress,
            RequestedByUserAgent = userAgent
        };

        await _passwordResetRequests.AddAsync(passwordResetRequest, cancellationToken);
        await _passwordResetRequests.SaveChangesAsync(cancellationToken);
        await _emailSender.SendAsync(
            account.Email,
            "Reset your IceBot password",
            BuildResetEmail(account.FullName, rawToken),
            cancellationToken);

        return ApiResult<bool>.Success(true, "If the account exists, password reset instructions have been sent.");
    }

    public async Task<ApiResult<bool>> ResetAsync(
        ResetPasswordRequest request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return ApiResult<bool>.Fail("Password reset token is required.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return ApiResult<bool>.Fail("New password is required.");
        }

        var tokenHash = HashToken(request.Token.Trim());
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

    private string BuildResetEmail(string? fullName, string rawToken)
    {
        var encodedToken = WebUtility.UrlEncode(rawToken);
        var resetUrl = string.IsNullOrWhiteSpace(_emailOptions.Value.PasswordResetBaseUrl)
            ? null
            : $"{_emailOptions.Value.PasswordResetBaseUrl.TrimEnd('/')}?token={encodedToken}";

        var displayName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(fullName) ? "there" : fullName.Trim());
        var tokenText = WebUtility.HtmlEncode(rawToken);
        var resetLink = resetUrl is null
            ? string.Empty
            : $"""<p><a href="{WebUtility.HtmlEncode(resetUrl)}">Reset password</a></p>""";

        return $"""
            <p>Hi {displayName},</p>
            <p>Use the token below to reset your IceBot password. This token expires in 30 minutes.</p>
            <p><strong>{tokenText}</strong></p>
            {resetLink}
            <p>If you did not request this, ignore this email.</p>
            """;
    }

    private static string CreateToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static string NormalizeEmailOrUserName(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Contains('@', StringComparison.Ordinal)
            ? trimmed.ToLowerInvariant()
            : trimmed.ToLowerInvariant();
    }
}
