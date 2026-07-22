using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Stores.Results;
using Domain.Common.Enums;

namespace Application.Tenants.Stores.Queries;

public sealed class ListStoresQueryHandler
{
    private readonly IStoreStore _storeStore;

    public ListStoresQueryHandler(IStoreStore storeStore)
    {
        _storeStore = storeStore;
    }

    public async Task<ApiResult<IReadOnlyList<StoreResult>>> HandleAsync(
        ListStoresQuery query,
        CancellationToken cancellationToken = default)
    {
        var userContext = query.UserContext;
        var organizationId = query.OrganizationId;
        var status = query.Status;
        var search = query.Search;
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.StoresView, userContext);

        EntityStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<EntityStatus>(status.Trim(), ignoreCase: true, out var resultStatus))
            {
                return ApiResult<IReadOnlyList<StoreResult>>.Fail("Invalid store status.", 400);
            }

            parsedStatus = resultStatus;
        }

        if (userContext.IsSystemAdmin)
        {
            var list = await _storeStore.ListAsync(organizationId, parsedStatus, search, cancellationToken);
            return ApiResult<IReadOnlyList<StoreResult>>.Success(list.Select(store => StoreResultMapper.ToResult(store)).ToList());
        }

        var accessibleStores = await _storeStore.ListAccessibleAsync(
            scope.OrganizationIds,
            scope.StoreIds,
            organizationId,
            parsedStatus,
            search,
            cancellationToken);

        return ApiResult<IReadOnlyList<StoreResult>>.Success(accessibleStores.Select(store => StoreResultMapper.ToResult(store)).ToList());
    }
}
