using Application.Identity.Abstractions;
using Application.Identity.Invitations.Results;
using Application.Identity.Tokens.Services;
using Application.Shared.Wrappers;
using Domain.Identity.Enums;
using Domain.Identity.ValueObjects;

namespace Application.Identity.Invitations.Commands;

public sealed class AcceptInvitationCommandHandler
{
    private readonly IAccountInvitationStore _invitationStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly RefreshTokenService _refreshTokens;

    public AcceptInvitationCommandHandler(
        IAccountInvitationStore invitationStore,
        IPasswordHasher passwordHasher,
        RefreshTokenService refreshTokens)
    {
        _invitationStore = invitationStore;
        _passwordHasher = passwordHasher;
        _refreshTokens = refreshTokens;
    }

    public async Task<ApiResult<AcceptInvitationResult>> HandleAsync(
        AcceptInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        var request = command.Request;
        var ipAddress = command.IpAddress;
        var userAgent = command.UserAgent;

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return ApiResult<AcceptInvitationResult>.Fail("Invitation token is required.", 400);
        }

        var tokenHash = AccountInvitationTokenHelper.HashToken(request.Token.Trim());
        var invitation = await _invitationStore.GetByTokenHashAsync(tokenHash, asNoTracking: false, cancellationToken);

        if (invitation is null)
        {
            return ApiResult<AcceptInvitationResult>.Fail("Invitation token is invalid.", 400);
        }

        if (invitation.AcceptedAt is not null)
        {
            return ApiResult<AcceptInvitationResult>.Fail("Invitation already accepted.", 400);
        }

        if (invitation.RevokedAt is not null)
        {
            return ApiResult<AcceptInvitationResult>.Fail("Invitation has been revoked. Please request a new invitation.", 400);
        }

        if (invitation.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return ApiResult<AcceptInvitationResult>.Fail("Invitation has expired. Please request a new invitation.", 400);
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
}
