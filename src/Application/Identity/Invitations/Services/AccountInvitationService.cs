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

        var created = await IssueInvitationRecordAsync(
            account,
            invitedByAccountId,
            replaceActiveInvitation: true,
            cancellationToken);
        var invitation = created.Invitation;
        var rawToken = created.RawToken;
        var invitationUrl = AccountInvitationUrlBuilder.BuildInvitationUrl(rawToken!, _emailOptions.Value.InvitationBaseUrl);

        var emailSent = await TrySendInvitationEmailAsync(account, invitation, rawToken!, sendEmail, cancellationToken);

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

    public async Task<ApiResult<InvitationEnsureResult>> EnsureActiveInvitationAsync(
        Account account,
        Guid? invitedByAccountId = null,
        bool sendEmail = true,
        CancellationToken cancellationToken = default)
    {
        var issued = await IssueInvitationRecordAsync(
            account,
            invitedByAccountId,
            replaceActiveInvitation: false,
            cancellationToken);

        if (!issued.Created)
            return ApiResult<InvitationEnsureResult>.Success(
                new InvitationEnsureResult { Created = false },
                "An active invitation already exists.");

        var emailSent = await TrySendInvitationEmailAsync(account, issued.Invitation, issued.RawToken!, sendEmail, cancellationToken);
        await _invitationStore.SaveChangesAsync(cancellationToken);

        return ApiResult<InvitationEnsureResult>.Success(
            new InvitationEnsureResult { Created = true },
            emailSent || !sendEmail
                ? "Invitation was created."
                : "Invitation was created, but the email could not be sent.");
    }

    private async Task<bool> TrySendInvitationEmailAsync(
        Account account,
        AccountInvitation invitation,
        string rawToken,
        bool sendEmail,
        CancellationToken cancellationToken)
    {
        if (!sendEmail)
        {
            return false;
        }

        try
        {
            var invitationUrl = AccountInvitationUrlBuilder.BuildInvitationUrl(rawToken, _emailOptions.Value.InvitationBaseUrl);
            await _emailSender.SendAsync(
                account.Email,
                "Complete your IceBot account setup",
                AccountInvitationEmailBuilder.BuildInvitationEmail(account.FullName, invitationUrl),
                cancellationToken);
            invitation.EmailSentAt = DateTimeOffset.UtcNow;
            return true;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Failed to send invitation email to {Email}", account.Email);
            return false;
        }
    }

    private Task<(AccountInvitation Invitation, string? RawToken, bool Created)> IssueInvitationRecordAsync(
        Account account,
        Guid? invitedByAccountId,
        bool replaceActiveInvitation,
        CancellationToken cancellationToken)
    {
        return _invitationStore.ExecuteCreationTransactionAsync(
            account.Id,
            async transactionToken =>
            {
                var activeInvitations = await _invitationStore.GetActiveInvitationsByAccountIdAsync(account.Id, transactionToken);
                var now = DateTimeOffset.UtcNow;
                if (!replaceActiveInvitation)
                {
                    var usableInvitation = activeInvitations.FirstOrDefault(invitation => invitation.ExpiresAt > now);
                    if (usableInvitation is not null)
                        return (usableInvitation, (string?)null, false);
                }

                foreach (var activeInvitation in activeInvitations) activeInvitation.RevokedAt = now;
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
                await _invitationStore.AddAsync(invitation, transactionToken);
                await _invitationStore.SaveChangesAsync(transactionToken);
                return (invitation, rawToken, true);
            },
            cancellationToken);
    }
}
