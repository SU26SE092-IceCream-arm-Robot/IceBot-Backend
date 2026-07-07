using Application.Inventory.Abstractions;
using Application.Inventory.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Inventory.Queries;

public sealed class GetDispenserRebindHistoryQueryHandler(IInventoryStore inventory)
{
    public async Task<ApiResult<IReadOnlyList<DispenserRebindHistoryResult>>> HandleAsync(
        GetDispenserRebindHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var state = await inventory.GetDispenserStateByIdAsync(query.DispenserStateId, cancellationToken);
        if (state?.Kiosk is null)
        {
            return ApiResult<IReadOnlyList<DispenserRebindHistoryResult>>.Fail("Dispenser state not found.", 404);
        }

        if (!ScopeAccessRules.CanAccessScopedRow(
                ScopeRoleSets.InventoryView,
                query.UserContext,
                state.Kiosk.OrganizationId,
                state.Kiosk.StoreId,
                state.KioskId))
        {
            return ApiResult<IReadOnlyList<DispenserRebindHistoryResult>>.Fail("Access denied.", 403);
        }

        var records = await inventory.ListTopologyRebindRecordsAsync(
            query.DispenserStateId,
            cancellationToken: cancellationToken);
        var results = records.Select(record => new DispenserRebindHistoryResult
        {
            Id = record.Id,
            SourceDispenserStateId = record.SourceDispenserStateId,
            ReplacementDispenserStateId = record.ReplacementDispenserStateId,
            SourceDeviceId = record.SourceDeviceId,
            ReplacementDeviceId = record.ReplacementDeviceId,
            SourceIngredientId = record.SourceIngredientId,
            ReplacementIngredientId = record.ReplacementIngredientId,
            SourceContainerCode = record.SourceContainerCode,
            ReplacementContainerCode = record.ReplacementContainerCode,
            EstimateDisposition = record.EstimateDisposition,
            PreviousEstimatedQuantity = record.PreviousEstimatedQuantity,
            TransferredQuantity = record.TransferredQuantity,
            SourceUnit = record.SourceUnit,
            ReplacementUnit = record.ReplacementUnit,
            Reason = record.Reason,
            ActorAccountId = record.CreatedByAccountId,
            ReboundAt = record.CreatedAt
        }).ToList();

        return ApiResult<IReadOnlyList<DispenserRebindHistoryResult>>.Success(results);
    }
}
