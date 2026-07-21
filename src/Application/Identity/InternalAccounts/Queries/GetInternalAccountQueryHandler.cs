using Application.Identity.Abstractions;
using Application.Identity.InternalAccounts.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Identity.InternalAccounts.Queries;

public sealed class GetInternalAccountQueryHandler
{
    private readonly IIdentityAccountStore _accounts;

    public GetInternalAccountQueryHandler(IIdentityAccountStore accounts)
    {
        _accounts = accounts;
    }

    public async Task<ApiResult<InternalAccountResult>> HandleAsync(
        GetInternalAccountQuery query,
        CancellationToken cancellationToken = default)
    {
        var account = await _accounts.GetByIdAsync(query.AccountId, cancellationToken: cancellationToken);
        if (account is null)
        {
            return ApiResult<InternalAccountResult>.Fail("Account not found.", 404);
        }

        if (!ScopeAccessRules.SharesAnyActiveScope(
                ScopeRoleSets.AccountsRead,
                query.UserContext,
                account.AccountRoles))
        {
            return ApiResult<InternalAccountResult>.Fail("Access denied.", 403);
        }

        return ApiResult<InternalAccountResult>.Success(InternalAccountResultMapper.ToResult(account));
    }
}
