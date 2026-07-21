using Application.Shared.Wrappers;
using Application.Tenants.Abstractions;
using Application.Tenants.Stores.Results;
using Domain.Common.Enums;

namespace Application.Tenants.Stores.Commands;

public sealed class PauseStoreSalesCommandHandler(IStoreStore storeStore)
{
    public async Task<ApiResult<StoreResult>> HandleAsync(
        PauseStoreSalesCommand command,
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
                ScopeRoleSets.StoresManage,
                command.UserContext,
                command.OrganizationId))
        {
            return ApiResult<StoreResult>.Fail("Access denied.", 403);
        }

        if (store.Status != EntityStatus.Active)
        {
            return ApiResult<StoreResult>.Fail(
                "Only an active store can be paused for sales.",
                409);
        }

        var reason = command.Request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 500)
        {
            return ApiResult<StoreResult>.Fail(
                "Sales pause reason is required and must be at most 500 characters.",
                400);
        }

        var now = DateTimeOffset.UtcNow;
        if (command.Request.ResumeAt.HasValue && command.Request.ResumeAt.Value <= now)
        {
            return ApiResult<StoreResult>.Fail("Sales resume time must be in the future.", 400);
        }

        store.PauseSales(now, command.UserContext.AccountId, reason, command.Request.ResumeAt);
        store.UpdatedAt = now;
        store.UpdatedByAccountId = command.UserContext.AccountId;
        await storeStore.SaveChangesAsync(cancellationToken);

        return ApiResult<StoreResult>.Success(
            StoreResultMapper.ToResult(store, now),
            "Store sales paused.");
    }
}
