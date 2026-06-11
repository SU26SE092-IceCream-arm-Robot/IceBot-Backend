using Application.Inventory.Abstractions;
using Application.Inventory.Mapping;
using Application.Inventory.Results;
using Application.Shared.Wrappers;

namespace Application.Inventory.Queries;

public sealed class GetStockMovementsQueryHandler
{
    private readonly IInventoryStore _inventoryStore;

    public GetStockMovementsQueryHandler(IInventoryStore inventoryStore)
    {
        _inventoryStore = inventoryStore;
    }

    public async Task<PagedResult<StockMovementResult>> HandleAsync(
        GetStockMovementsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var totalCount = await _inventoryStore.CountStockMovementsAsync(
            query.OrganizationId,
            query.StoreId,
            query.KioskId,
            query.UserContext.IsSystemAdmin,
            query.UserContext.AllowedOrganizationIds,
            query.UserContext.AllowedStoreIds,
            query.UserContext.AllowedKioskIds,
            cancellationToken);

        var list = await _inventoryStore.ListStockMovementsAsync(
            query.OrganizationId,
            query.StoreId,
            query.KioskId,
            query.UserContext.IsSystemAdmin,
            query.UserContext.AllowedOrganizationIds,
            query.UserContext.AllowedStoreIds,
            query.UserContext.AllowedKioskIds,
            pageNumber,
            pageSize,
            cancellationToken);

        return PagedResult<StockMovementResult>.Success(
            list.Select(StockMovementResultMapper.ToResult),
            totalCount,
            pageNumber,
            pageSize,
            "Stock movements retrieved successfully.");
    }
}
