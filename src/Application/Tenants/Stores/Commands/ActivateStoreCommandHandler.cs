using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Domain.Common.Enums;

namespace Application.Tenants.Stores.Commands;

public sealed class ActivateStoreCommandHandler
{
    private readonly IStoreStore _storeStore;

    public ActivateStoreCommandHandler(IStoreStore storeStore)
    {
        _storeStore = storeStore;
    }

    public async Task<ApiResult<bool>> HandleAsync(
        ActivateStoreCommand command,
        CancellationToken cancellationToken = default)
    {
        var userContext = command.UserContext;
        var storeId = command.StoreId;

        var store = await _storeStore.GetByIdAsync(storeId, cancellationToken);
        if (store is null)
        {
            return ApiResult<bool>.Fail("Store not found.", 404);
        }

        if (!StoreAccessRules.CanManageOrganizationStores(userContext, store.OrganizationId))
        {
            return ApiResult<bool>.Fail("Access denied.", 403);
        }

        var isOrgActive = await _storeStore.OrganizationExistsActiveAsync(store.OrganizationId, cancellationToken);
        if (!isOrgActive)
        {
            return ApiResult<bool>.Fail("Parent organization is inactive or does not exist.", 400);
        }

        store.Status = EntityStatus.Active;
        store.UpdatedAt = DateTimeOffset.UtcNow;
        store.UpdatedByAccountId = userContext.AccountId;

        await _storeStore.SaveChangesAsync(cancellationToken);

        return ApiResult<bool>.Success(true, "Store activated successfully.");
    }
}
