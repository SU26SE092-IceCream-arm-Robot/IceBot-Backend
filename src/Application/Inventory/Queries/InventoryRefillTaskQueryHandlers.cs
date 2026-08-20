using Application.Inventory.Abstractions;
using Application.Inventory.Mapping;
using Application.Inventory.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Inventory.Queries;

public sealed class ListInventoryRefillTasksQueryHandler(IInventoryStore inventory)
{
    public async Task<PagedResult<InventoryRefillTaskResult>> HandleAsync(ListInventoryRefillTasksQuery query, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        if (query.RequestedFrom.HasValue && query.RequestedTo.HasValue && query.RequestedFrom >= query.RequestedTo)
            return PagedResult<InventoryRefillTaskResult>.Fail("Requested-from must be earlier than requested-to.", 400, pageNumber, pageSize);

        var kiosk = await inventory.GetKioskForInventoryTopologyAsync(query.KioskId, cancellationToken);
        if (kiosk is null) return PagedResult<InventoryRefillTaskResult>.Fail("Kiosk was not found.", 404, pageNumber, pageSize);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.InventoryView, query.UserContext, kiosk.OrganizationId, kiosk.StoreId, kiosk.Id))
            return PagedResult<InventoryRefillTaskResult>.Forbidden("Access denied.", pageNumber, pageSize);

        var totalCount = await inventory.CountInventoryRefillTasksAsync(query.KioskId, query.Status, query.RequestedFrom, query.RequestedTo, cancellationToken);
        var tasks = await inventory.ListInventoryRefillTasksAsync(query.KioskId, query.Status, query.RequestedFrom, query.RequestedTo, pageNumber, pageSize, cancellationToken);
        return PagedResult<InventoryRefillTaskResult>.Success(tasks.Select(InventoryRefillTaskResultMapper.ToResult), totalCount, pageNumber, pageSize, "Inventory refill tasks retrieved successfully.");
    }
}

public sealed class GetInventoryRefillTaskQueryHandler(IInventoryStore inventory)
{
    public async Task<ApiResult<InventoryRefillTaskResult>> HandleAsync(GetInventoryRefillTaskQuery query, CancellationToken cancellationToken = default)
    {
        var task = await inventory.GetInventoryRefillTaskAsync(query.TaskId, cancellationToken);
        if (task is null || task.KioskId != query.KioskId) return ApiResult<InventoryRefillTaskResult>.Fail("Inventory refill task was not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.InventoryView, query.UserContext, task.OrganizationId, task.StoreId, task.KioskId)) return ApiResult<InventoryRefillTaskResult>.Fail("Access denied.", 403);
        return ApiResult<InventoryRefillTaskResult>.Success(InventoryRefillTaskResultMapper.ToResult(task));
    }
}
