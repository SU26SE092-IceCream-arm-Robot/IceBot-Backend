using Application.Identity.Abstractions;
using Application.Identity.Access.Mapping;
using Application.Identity.Access.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Identity.InternalAccounts.Queries;

public sealed class GetInternalAccountEffectiveAccessQueryHandler
{
    private readonly IIdentityAccountStore _accounts;

    public GetInternalAccountEffectiveAccessQueryHandler(IIdentityAccountStore accounts)
    {
        _accounts = accounts;
    }

    public async Task<ApiResult<AccountAccessResult>> HandleAsync(
        GetInternalAccountEffectiveAccessQuery query,
        CancellationToken cancellationToken = default)
    {
        var account = await _accounts.GetByIdAsync(query.AccountId, asNoTracking: true, cancellationToken: cancellationToken);
        if (account is null)
        {
            return ApiResult<AccountAccessResult>.Fail("Account not found.", 404);
        }

        if (!ScopeAccessRules.SharesAnyActiveScope(query.UserContext, account.AccountRoles))
        {
            return ApiResult<AccountAccessResult>.Fail("Access denied.", 403);
        }

        return ApiResult<AccountAccessResult>.Success(
            AccountAccessResultMapper.FromAccount(account),
            "Effective access retrieved successfully.");
    }
}
