using Application.Inventory.Abstractions;
using Application.Inventory.Mapping;
using Application.Inventory.Results;
using Application.Shared.Wrappers;

namespace Application.Inventory.Queries;

public sealed class GetDispenserStatesQueryHandler
{
    private readonly IInventoryStore _inventoryStore;

    public GetDispenserStatesQueryHandler(IInventoryStore inventoryStore)
    {
        _inventoryStore = inventoryStore;
    }

    public async Task<PagedResult<DispenserStateResult>> HandleAsync(
        GetDispenserStatesQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var totalCount = await _inventoryStore.CountDispenserStatesAsync(
            query.OrganizationId,
            query.StoreId,
            query.KioskId,
            query.IsActive,
            query.UserContext.IsSystemAdmin,
            query.UserContext.AllowedOrganizationIds,
            query.UserContext.AllowedStoreIds,
            query.UserContext.AllowedKioskIds,
            cancellationToken);

        var list = await _inventoryStore.ListDispenserStatesAsync(
            query.OrganizationId,
            query.StoreId,
            query.KioskId,
            query.IsActive,
            query.UserContext.IsSystemAdmin,
            query.UserContext.AllowedOrganizationIds,
            query.UserContext.AllowedStoreIds,
            query.UserContext.AllowedKioskIds,
            pageNumber,
            pageSize,
            cancellationToken);

        return PagedResult<DispenserStateResult>.Success(
            list.Select(DispenserStateResultMapper.ToResult),
            totalCount,
            pageNumber,
            pageSize,
            "Dispenser states retrieved successfully.");
    }
}
