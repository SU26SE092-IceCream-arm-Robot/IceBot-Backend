using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Domain.Common.Enums;

namespace Application.Tenants.Stores.Commands;

public sealed class DisableStoreCommandHandler
{
    private readonly IStoreStore _storeStore;

    public DisableStoreCommandHandler(IStoreStore storeStore)
    {
        _storeStore = storeStore;
    }

    public async Task<ApiResult<bool>> HandleAsync(
        DisableStoreCommand command,
        CancellationToken cancellationToken = default)
    {
        var userContext = command.UserContext;
        var storeId = command.StoreId;

        var store = await _storeStore.GetByIdAsync(storeId, cancellationToken);
        if (store is null)
        {
            return ApiResult<bool>.Fail("Store not found.", 404);
        }

        if (!StoreAccessRules.CanManageOrganizationStores(
                ScopeRoleSets.StoresManage, userContext, store.OrganizationId))
        {
            return ApiResult<bool>.Fail("Access denied.", 403);
        }

        store.Status = EntityStatus.Inactive;
        store.UpdatedAt = DateTimeOffset.UtcNow;
        store.UpdatedByAccountId = userContext.AccountId;

        await _storeStore.SaveChangesAsync(cancellationToken);

        return ApiResult<bool>.Success(true, "Store disabled successfully.");
    }
}
