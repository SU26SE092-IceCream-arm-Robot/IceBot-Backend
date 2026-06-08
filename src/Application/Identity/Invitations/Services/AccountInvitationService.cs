using Application.Email;
using Application.Identity.Abstractions;
using Application.Identity.Invitations.Requests;
using Application.Identity.Invitations.Results;
using Application.Identity.Tokens.Services;
using Application.Shared.Wrappers;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Identity.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Identity.Invitations.Services;

public sealed class AccountInvitationService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(7);

    private readonly IAccountInvitationStore _invitationStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly IOptions<EmailOptions> _emailOptions;
    private readonly RefreshTokenService _refreshTokens;
    private readonly ILogger<AccountInvitationService> _logger;

    public AccountInvitationService(
        IAccountInvitationStore invitationStore,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        IOptions<EmailOptions> emailOptions,
        RefreshTokenService refreshTokens,
        ILogger<AccountInvitationService> logger)
    {
        _invitationStore = invitationStore;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _emailOptions = emailOptions;
        _refreshTokens = refreshTokens;
        _logger = logger;
    }

    public async Task<ApiResult<AccountInvitationResult>> CreateInvitationAsync(
        Account account,
        Guid? invitedByAccountId = null,
        bool sendEmail = true,
        CancellationToken cancellationToken = default)
    {
        if (account is null)
        {
            return ApiResult<AccountInvitationResult>.Fail("Account is required.");
        }

        if (string.IsNullOrWhiteSpace(account.Email))
        {
            return ApiResult<AccountInvitationResult>.Fail("Account email is required.");
        }

        var activeInvitations = await _invitationStore.GetActiveInvitationsByAccountIdAsync(account.Id, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var rawToken = CreateToken();
        var invitation = new AccountInvitation
        {
            AccountId = account.Id,
            TokenHash = HashToken(rawToken),
            InvitedAt = now,
            ExpiresAt = now.Add(TokenLifetime),
            InvitedByAccountId = invitedByAccountId,
            Purpose = "AccountInvitation"
        };

        await _invitationStore.AddAsync(invitation, cancellationToken);
        await _invitationStore.SaveChangesAsync(cancellationToken);

        var invitationUrl = BuildInvitationUrl(rawToken);
        var emailSent = false;

        if (sendEmail)
        {
            try
            {
                await _emailSender.SendAsync(
                    account.Email,
                    "Complete your IceBot account setup",
                    BuildInvitationEmail(account.FullName, rawToken, invitationUrl),
                    cancellationToken);

                emailSent = true;
                invitation.EmailSentAt = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send invitation email to {Email}", account.Email);
            }
        }

        foreach (var activeInvitation in activeInvitations)
        {
            activeInvitation.RevokedAt = DateTimeOffset.UtcNow;
        }

        await _invitationStore.SaveChangesAsync(cancellationToken);

        var result = new AccountInvitationResult
        {
            AccountId = account.Id,
            InvitationToken = rawToken,
            InvitationUrl = invitationUrl,
            ExpiresAt = invitation.ExpiresAt,
            EmailSentAt = invitation.EmailSentAt,
            EmailSent = emailSent
        };

        if (sendEmail && !emailSent)
        {
            return ApiResult<AccountInvitationResult>.Success(
                result,
                "Invitation link created, but the invitation email could not be sent. Please check email configuration or send the link manually.",
                201);
        }

        var message = emailSent
            ? "Invitation link created and email sent."
            : "Invitation link created.";

        return ApiResult<AccountInvitationResult>.Success(result, message, 201);
    }

    public async Task<ApiResult<AcceptInvitationResult>> AcceptAsync(
        AcceptInvitationRequest request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return ApiResult<AcceptInvitationResult>.Fail("Invitation token is required.", 400);
        }

        var tokenHash = HashToken(request.Token.Trim());
        var invitation = await _invitationStore.GetByTokenHashAsync(tokenHash, asNoTracking: false, cancellationToken);

        if (invitation is null ||
            invitation.AcceptedAt is not null ||
            invitation.RevokedAt is not null ||
            invitation.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return ApiResult<AcceptInvitationResult>.Fail("Invitation token is invalid or expired.", 400);
        }

        var account = invitation.Account;
        if (account is null)
        {
            return ApiResult<AcceptInvitationResult>.Fail("Account not found.", 404);
        }

        if (account.Status == AccountStatus.Disabled || account.Status == AccountStatus.Suspended)
        {
            return ApiResult<AcceptInvitationResult>.Fail("Account is disabled or suspended.", 403);
        }

        if (account.Status != AccountStatus.Invited)
        {
            invitation.RevokedAt = DateTimeOffset.UtcNow;
            await _invitationStore.SaveChangesAsync(cancellationToken);

            return ApiResult<AcceptInvitationResult>.Fail("Invitation token is no longer valid for this account.", 400);
        }

        if (account.LocalLoginEnabled)
        {
            if (account.Password is null && string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return ApiResult<AcceptInvitationResult>.Fail("New password is required for local login.", 400);
            }

            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                account.Password = HashedPassword.From(_passwordHasher.HashPassword(request.NewPassword));
            }
        }

        account.Status = AccountStatus.Active;
        if (invitation.EmailSentAt is not null)
        {
            account.EmailConfirmed = true;
            account.EmailConfirmedAt = DateTimeOffset.UtcNow;
        }

        account.UpdatedAt = DateTimeOffset.UtcNow;
        account.UpdatedByAccountId = account.Id;

        invitation.AcceptedAt = DateTimeOffset.UtcNow;
        invitation.AcceptedByIp = ipAddress;
        invitation.AcceptedByUserAgent = userAgent;

        await _invitationStore.SaveChangesAsync(cancellationToken);

        // Revoke any existing sessions/refresh tokens for security
        await _refreshTokens.RevokeAllForAccountAsync(account.Id, "Invitation accepted", ipAddress, userAgent);

        return ApiResult<AcceptInvitationResult>.Success(new AcceptInvitationResult
        {
            Accepted = true,
            LocalLoginEnabled = account.LocalLoginEnabled,
            GoogleLoginEnabled = account.GoogleLoginEnabled
        }, "Invitation accepted and account activated.");
    }

    private string BuildInvitationEmail(string? fullName, string rawToken, string? invitationUrl)
    {
        var displayName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(fullName) ? "there" : fullName.Trim());
        var tokenText = WebUtility.HtmlEncode(rawToken);
        var inviteLink = invitationUrl is null
            ? string.Empty
            : $"""<p><a href="{WebUtility.HtmlEncode(invitationUrl)}">Set up your account</a></p>""";

        return $"""
            <p>Hi {displayName},</p>
            <p>You have been invited to join IceBot. Use the link or the code below to complete your account setup. This link is valid for 7 days.</p>
            <p>Code: <strong>{tokenText}</strong></p>
            {inviteLink}
            <p>If you did not expect this invitation, you can safely ignore this email.</p>
            """;
    }

    private string? BuildInvitationUrl(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(_emailOptions.Value.InvitationBaseUrl))
        {
            return null;
        }

        var encodedToken = WebUtility.UrlEncode(rawToken);
        return $"{_emailOptions.Value.InvitationBaseUrl.TrimEnd('/')}?token={encodedToken}";
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
}
