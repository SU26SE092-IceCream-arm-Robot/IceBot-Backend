using Application.Identity.Abstractions;
using Application.Identity.InternalAccounts.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Identity.InternalAccounts.Queries;

public sealed class ListInternalAccountsQueryHandler
{
    private readonly IIdentityAccountStore _accounts;

    public ListInternalAccountsQueryHandler(IIdentityAccountStore accounts)
    {
        _accounts = accounts;
    }

    public async Task<PagedResult<InternalAccountResult>> HandleAsync(
        ListInternalAccountsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.AccountsRead, query.UserContext);

        var totalCount = await _accounts.CountAsync(
            query.Search,
            query.Status,
            query.UserContext.IsSystemAdmin,
            scope.OrganizationIds,
            scope.StoreIds,
            scope.KioskIds,
            cancellationToken);

        var accounts = await _accounts.ListAsync(
            query.Search,
            query.Status,
            query.UserContext.IsSystemAdmin,
            scope.OrganizationIds,
            scope.StoreIds,
            scope.KioskIds,
            pageNumber,
            pageSize,
            cancellationToken);
        return PagedResult<InternalAccountResult>.Success(
            accounts.Select(account => InternalAccountResultMapper.ToResult(account)),
            totalCount,
            pageNumber,
            pageSize);
    }
}
