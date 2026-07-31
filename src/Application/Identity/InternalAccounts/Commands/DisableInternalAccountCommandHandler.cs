using Application.Identity.Abstractions;
using Application.Identity.InternalAccounts.Results;
using Application.Identity.Tokens.Services;
using Application.Shared.Wrappers;
using Domain.Identity.Enums;

namespace Application.Identity.InternalAccounts.Commands;

public sealed class DisableInternalAccountCommandHandler
{
    private readonly IIdentityAccountStore _accounts;
    private readonly RefreshTokenService _refreshTokens;

    public DisableInternalAccountCommandHandler(
        IIdentityAccountStore accounts,
        RefreshTokenService refreshTokens)
    {
        _accounts = accounts;
        _refreshTokens = refreshTokens;
    }

    public async Task<ApiResult<InternalAccountResult>> HandleAsync(
        DisableInternalAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        var accountId = command.AccountId;
        var updatedByAccountId = command.UpdatedByAccountId;

        var account = await _accounts.GetByIdAsync(accountId, asNoTracking: false, cancellationToken: cancellationToken);
        if (account is null)
        {
            return ApiResult<InternalAccountResult>.Fail("Account not found.", 404);
        }

        if (!AccountManagementAccessRules.CanManageAccount(command.UserContext, command.OrganizationId, account))
        {
            return ApiResult<InternalAccountResult>.Fail("Account is outside the current organization's management scope.", 403);
        }

        account.Status = AccountStatus.Disabled;
        account.UpdatedAt = DateTimeOffset.UtcNow;
        account.UpdatedByAccountId = updatedByAccountId;
        await _accounts.SaveChangesAsync(cancellationToken);
        await _refreshTokens.RevokeAllForAccountAsync(account.Id, "Account disabled by admin", null);

        return ApiResult<InternalAccountResult>.Success(
            InternalAccountResultMapper.ToResult(account, organizationId: command.OrganizationId),
            "Internal account disabled.");
    }
}
