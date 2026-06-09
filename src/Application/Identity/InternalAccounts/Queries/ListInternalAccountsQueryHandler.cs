using Application.Identity.Abstractions;
using Application.Identity.InternalAccounts.Results;
using Application.Shared.Wrappers;

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

        var totalCount = await _accounts.CountAsync(
            query.Search,
            query.Status,
            query.UserContext.IsSystemAdmin,
            query.UserContext.AllowedOrganizationIds,
            query.UserContext.AllowedStoreIds,
            query.UserContext.AllowedKioskIds,
            cancellationToken);

        var accounts = await _accounts.ListAsync(
            query.Search,
            query.Status,
            query.UserContext.IsSystemAdmin,
            query.UserContext.AllowedOrganizationIds,
            query.UserContext.AllowedStoreIds,
            query.UserContext.AllowedKioskIds,
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
