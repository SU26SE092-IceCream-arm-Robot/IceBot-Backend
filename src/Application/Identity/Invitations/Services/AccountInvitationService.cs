using Application.Email;
using Application.Identity.Abstractions;
using Application.Identity.Invitations.Results;
using Application.Shared.Wrappers;
using Domain.Identity.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Identity.Invitations.Services;

public sealed class AccountInvitationService
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(7);

    private readonly IAccountInvitationStore _invitationStore;
    private readonly IEmailSender _emailSender;
    private readonly IOptions<EmailOptions> _emailOptions;
    private readonly ILogger<AccountInvitationService> _logger;

    public AccountInvitationService(
        IAccountInvitationStore invitationStore,
        IEmailSender emailSender,
        IOptions<EmailOptions> emailOptions,
        ILogger<AccountInvitationService> logger)
    {
        _invitationStore = invitationStore;
        _emailSender = emailSender;
        _emailOptions = emailOptions;
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

        foreach (var activeInvitation in activeInvitations)
        {
            activeInvitation.RevokedAt = now;
        }

        var rawToken = AccountInvitationTokenHelper.CreateToken();
        var invitation = new AccountInvitation
        {
            AccountId = account.Id,
            TokenHash = AccountInvitationTokenHelper.HashToken(rawToken),
            InvitedAt = now,
            ExpiresAt = now.Add(TokenLifetime),
            InvitedByAccountId = invitedByAccountId,
            Purpose = "AccountInvitation"
        };

        await _invitationStore.AddAsync(invitation, cancellationToken);
        await _invitationStore.SaveChangesAsync(cancellationToken);

        var invitationUrl = AccountInvitationUrlBuilder.BuildInvitationUrl(rawToken, _emailOptions.Value.InvitationBaseUrl);
        var emailSent = false;

        if (sendEmail)
        {
            try
            {
                await _emailSender.SendAsync(
                    account.Email,
                    "Complete your IceBot account setup",
                    AccountInvitationEmailBuilder.BuildInvitationEmail(account.FullName, invitationUrl),
                    cancellationToken);

                emailSent = true;
                invitation.EmailSentAt = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send invitation email to {Email}", account.Email);
            }
        }

        await _invitationStore.SaveChangesAsync(cancellationToken);

        var result = new AccountInvitationResult
        {
            AccountId = account.Id,
            InvitationUrl = invitationUrl,
            ExpiresAt = invitation.ExpiresAt,
            EmailSentAt = invitation.EmailSentAt
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
}
