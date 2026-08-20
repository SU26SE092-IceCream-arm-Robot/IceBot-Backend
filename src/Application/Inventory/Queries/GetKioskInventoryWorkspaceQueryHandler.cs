using Application.Identity.Tokens.Claims;
using Application.Inventory.Abstractions;
using Application.Inventory.Mapping;
using Application.Inventory.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Inventory.Enums;

namespace Application.Inventory.Queries;

public sealed record GetKioskInventoryWorkspaceQuery(Guid KioskId, CurrentUserContext UserContext);

public sealed class GetKioskInventoryWorkspaceQueryHandler(IInventoryStore inventory)
{
    private const int ActiveRefillTaskLimit = 20;

    public async Task<ApiResult<KioskInventoryWorkspaceResult>> HandleAsync(
        GetKioskInventoryWorkspaceQuery query,
        CancellationToken cancellationToken = default)
    {
        var kiosk = await inventory.GetKioskForInventoryTopologyAsync(query.KioskId, cancellationToken);
        if (kiosk is null)
        {
            return ApiResult<KioskInventoryWorkspaceResult>.Fail("Kiosk was not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.InventoryView,
                query.UserContext,
                kiosk.OrganizationId,
                kiosk.StoreId,
                kiosk.Id))
        {
            return ApiResult<KioskInventoryWorkspaceResult>.Fail("Access denied.", 403);
        }

        var balancesTask = inventory.ListKioskIngredientInventoriesAsync(kiosk.Id, cancellationToken);
        var activeTasksTask = inventory.ListActiveInventoryRefillTasksAsync(
            kiosk.Id,
            ActiveRefillTaskLimit,
            cancellationToken);
        await Task.WhenAll(balancesTask, activeTasksTask);

        var activeTasks = (await activeTasksTask)
            .Select(InventoryRefillTaskResultMapper.ToResult)
            .ToArray();
        var activeTaskByInventory = activeTasks
            .GroupBy(task => task.KioskIngredientInventoryId)
            .ToDictionary(group => group.Key, group => group.First());
        var now = DateTimeOffset.UtcNow;
        var inventories = (await balancesTask)
            .Select(KioskIngredientInventoryResultMapper.ToResult)
            .Select(inventory => new KioskInventoryWorkspaceInventoryResult
            {
                Inventory = inventory,
                InventoryStatus = GetInventoryStatus(inventory, now),
                ActiveRefillTask = activeTaskByInventory.GetValueOrDefault(inventory.Id)
            })
            .ToArray();

        var canManageRefill = ScopeAccessRules.CanAccessScopedRow(
            ScopeRoleSets.InventoryRefillManage,
            query.UserContext,
            kiosk.OrganizationId,
            kiosk.StoreId,
            kiosk.Id);
        var canAdjustInventory = ScopeAccessRules.CanAccessScopedRow(
            ScopeRoleSets.InventoryAdjustManage,
            query.UserContext,
            kiosk.OrganizationId,
            kiosk.StoreId,
            kiosk.Id);
        var canConfigureInventory = ScopeAccessRules.CanAccessScopedRow(
            ScopeRoleSets.InventoryConfigure,
            query.UserContext,
            kiosk.OrganizationId,
            kiosk.StoreId,
            kiosk.Id);

        return ApiResult<KioskInventoryWorkspaceResult>.Success(new KioskInventoryWorkspaceResult
        {
            Summary = new KioskInventoryWorkspaceSummaryResult
            {
                InventoryCount = inventories.Length,
                LowInventoryCount = inventories.Count(item => item.InventoryStatus == KioskInventoryLevelStatus.Low),
                EmptyInventoryCount = inventories.Count(item => item.InventoryStatus == KioskInventoryLevelStatus.Empty),
                ExpiredInventoryCount = inventories.Count(item => item.InventoryStatus == KioskInventoryLevelStatus.Expired),
                ActiveRefillTaskCount = activeTasks.Length
            },
            Inventories = inventories,
            ActiveRefillTasks = activeTasks,
            AvailableActions = new KioskInventoryWorkspaceActionsResult
            {
                CanManageRefill = canManageRefill,
                CanAdjustInventory = canAdjustInventory,
                CanConfigureInventory = canConfigureInventory,
                StartableRefillTaskStatuses = canManageRefill ? [InventoryRefillTaskStatus.Requested] : [],
                CompletableRefillTaskStatuses = canManageRefill ? [InventoryRefillTaskStatus.InProgress] : []
            }
        });
    }

    private static KioskInventoryLevelStatus GetInventoryStatus(
        KioskIngredientInventoryResult inventory,
        DateTimeOffset now)
    {
        if (!inventory.IsActive || !inventory.EstimatedQuantity.HasValue)
        {
            return KioskInventoryLevelStatus.Unknown;
        }

        if (inventory.ExpiresAt.HasValue && inventory.ExpiresAt.Value <= now)
        {
            return KioskInventoryLevelStatus.Expired;
        }

        if (inventory.EstimatedQuantity.Value <= 0)
        {
            return KioskInventoryLevelStatus.Empty;
        }

        if (inventory.LowStockThreshold.HasValue && inventory.EstimatedQuantity.Value <= inventory.LowStockThreshold.Value)
        {
            return KioskInventoryLevelStatus.Low;
        }

        return KioskInventoryLevelStatus.InStock;
    }
}
