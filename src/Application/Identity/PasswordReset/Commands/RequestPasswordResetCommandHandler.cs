using Application.Email;
using Application.Identity.Abstractions;
using Application.Shared.Wrappers;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Microsoft.Extensions.Options;

namespace Application.Identity.PasswordReset.Commands;

public sealed class RequestPasswordResetCommandHandler
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(30);

    private readonly IIdentityAccountStore _accounts;
    private readonly IPasswordResetRequestStore _passwordResetRequests;
    private readonly IEmailSender _emailSender;
    private readonly IOptions<EmailOptions> _emailOptions;

    public RequestPasswordResetCommandHandler(
        IIdentityAccountStore accounts,
        IPasswordResetRequestStore passwordResetRequests,
        IEmailSender emailSender,
        IOptions<EmailOptions> emailOptions)
    {
        _accounts = accounts;
        _passwordResetRequests = passwordResetRequests;
        _emailSender = emailSender;
        _emailOptions = emailOptions;
    }

    public async Task<ApiResult<bool>> HandleAsync(
        RequestPasswordResetCommand command,
        CancellationToken cancellationToken = default)
    {
        var request = command.Request;
        var ipAddress = command.IpAddress;
        var userAgent = command.UserAgent;

        if (string.IsNullOrWhiteSpace(request.EmailOrUserName))
        {
            return ApiResult<bool>.Fail("Email or username is required.");
        }

        var login = PasswordResetRequestNormalizer.NormalizeEmailOrUserName(request.EmailOrUserName);
        var account = await _accounts.GetByEmailOrUserNameAsync(login, asNoTracking: false, cancellationToken);

        // Avoid account enumeration. Always return success for unknown or ineligible accounts.
        if (account is null ||
            account.Status != AccountStatus.Active ||
            !account.LocalLoginEnabled ||
            string.IsNullOrWhiteSpace(account.Email))
        {
            return ApiResult<bool>.Success(true, "If the account exists, password reset instructions have been sent.");
        }

        var rawToken = PasswordResetTokenHelper.CreateToken();
        var passwordResetRequest = new PasswordResetRequest
        {
            AccountId = account.Id,
            TokenHash = PasswordResetTokenHelper.HashToken(rawToken),
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
            PasswordResetEmailBuilder.BuildResetEmail(account.FullName, rawToken, _emailOptions.Value),
            cancellationToken);

        return ApiResult<bool>.Success(true, "If the account exists, password reset instructions have been sent.");
    }
}
