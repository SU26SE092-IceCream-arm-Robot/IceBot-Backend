using Application.Identity.Abstractions;
using Application.Identity.InternalAccounts.Results;
using Application.Identity.Tokens.Services;
using Application.Shared.Wrappers;
using Domain.Identity.ValueObjects;

namespace Application.Identity.InternalAccounts.Commands;

public sealed class SetInternalAccountPasswordCommandHandler
{
    private readonly IIdentityAccountStore _accounts;
    private readonly IPasswordHasher _passwordHasher;
    private readonly RefreshTokenService _refreshTokens;

    public SetInternalAccountPasswordCommandHandler(
        IIdentityAccountStore accounts,
        IPasswordHasher passwordHasher,
        RefreshTokenService refreshTokens)
    {
        _accounts = accounts;
        _passwordHasher = passwordHasher;
        _refreshTokens = refreshTokens;
    }

    public async Task<ApiResult<InternalAccountResult>> HandleAsync(
        SetInternalAccountPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var accountId = command.AccountId;
        var request = command.Request;
        var updatedByAccountId = command.UpdatedByAccountId;

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return ApiResult<InternalAccountResult>.Fail("New password is required.");
        }

        var account = await _accounts.GetTenantManagedByIdAsync(
            accountId,
            command.OrganizationId,
            asNoTracking: false,
            cancellationToken: cancellationToken);
        if (account is null)
        {
            return ApiResult<InternalAccountResult>.Fail("Account not found.", 404);
        }

        if (!AccountManagementAccessRules.CanManageAccount(command.UserContext, command.OrganizationId, account))
        {
            return ApiResult<InternalAccountResult>.Fail("Account is outside the current organization's management scope.", 403);
        }

        account.Password = HashedPassword.From(_passwordHasher.HashPassword(request.NewPassword));
        account.LockedUntil = null;
        account.FailedLoginCount = 0;
        account.UpdatedAt = DateTimeOffset.UtcNow;
        account.UpdatedByAccountId = updatedByAccountId;
        await _accounts.SaveChangesAsync(cancellationToken);
        await _refreshTokens.RevokeAllForAccountAsync(account.Id, "Password changed by admin", null);

        return ApiResult<InternalAccountResult>.Success(
            InternalAccountResultMapper.ToResult(account, organizationId: command.OrganizationId),
            "Password updated.");
    }
}
