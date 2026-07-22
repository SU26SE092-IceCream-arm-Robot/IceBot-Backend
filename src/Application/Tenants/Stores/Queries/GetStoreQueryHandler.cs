using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Stores.Results;

namespace Application.Tenants.Stores.Queries;

public sealed class GetStoreQueryHandler
{
    private readonly IStoreStore _storeStore;

    public GetStoreQueryHandler(IStoreStore storeStore)
    {
        _storeStore = storeStore;
    }

    public async Task<ApiResult<StoreResult>> HandleAsync(
        GetStoreQuery query,
        CancellationToken cancellationToken = default)
    {
        var store = await _storeStore.GetByIdAsync(query.StoreId, cancellationToken);
        if (store is null)
        {
            return ApiResult<StoreResult>.Fail("Store not found.", 404);
        }

        if (!StoreAccessRules.CanAccessStore(ScopeRoleSets.StoresView, query.UserContext, store))
        {
            return ApiResult<StoreResult>.Fail("Access denied.", 403);
        }

        return ApiResult<StoreResult>.Success(StoreResultMapper.ToResult(store));
    }
}
