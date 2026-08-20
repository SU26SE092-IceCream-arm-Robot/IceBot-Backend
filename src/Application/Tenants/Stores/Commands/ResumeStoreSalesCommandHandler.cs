using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Stores.Results;

namespace Application.Tenants.Stores.Commands;

public sealed class ResumeStoreSalesCommandHandler(IStoreStore storeStore)
{
    public async Task<ApiResult<StoreResult>> HandleAsync(
        ResumeStoreSalesCommand command,
        CancellationToken cancellationToken = default)
    {
        var store = await storeStore.GetByOrganizationAndIdAsync(
            command.OrganizationId,
            command.StoreId,
            cancellationToken);
        if (store is null)
        {
            return ApiResult<StoreResult>.Fail("Store not found.", 404);
        }

        if (!StoreAccessRules.CanManageOrganizationStores(
                ScopeRoleSets.StoresSalesManage,
                command.UserContext,
                command.OrganizationId))
        {
            return ApiResult<StoreResult>.Fail("Access denied.", 403);
        }

        var now = DateTimeOffset.UtcNow;
        store.ResumeSales(now, command.UserContext.AccountId);
        store.UpdatedAt = now;
        store.UpdatedByAccountId = command.UserContext.AccountId;
        await storeStore.SaveChangesAsync(cancellationToken);

        return ApiResult<StoreResult>.Success(
            StoreResultMapper.ToResult(store, now),
            "Store sales resumed.");
    }
}
