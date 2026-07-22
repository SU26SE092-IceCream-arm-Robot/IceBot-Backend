using Application.Inventory.Abstractions;
using Application.Inventory.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Inventory.Queries;

public sealed class GetInventorySummaryQueryHandler
{
    private readonly IInventoryStore _inventoryStore;

    public GetInventorySummaryQueryHandler(IInventoryStore inventoryStore)
    {
        _inventoryStore = inventoryStore;
    }

    public async Task<ApiResult<InventorySummaryResult>> HandleAsync(
        GetInventorySummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        var userContext = query.UserContext;
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.InventoryView, userContext);

        var summary = await _inventoryStore.GetInventorySummaryAsync(
            query.KioskId,
            query.StoreId,
            userContext.IsSystemAdmin,
            scope.OrganizationIds,
            scope.StoreIds,
            scope.KioskIds,
            cancellationToken);

        return ApiResult<InventorySummaryResult>.Success(summary, "Inventory summary retrieved successfully.");
    }
}
