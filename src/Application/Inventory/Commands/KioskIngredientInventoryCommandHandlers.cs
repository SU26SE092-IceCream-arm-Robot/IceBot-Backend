using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Inventory.Abstractions;
using Application.Inventory.Mapping;
using Application.Inventory.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.Inventory.Entities;
using Domain.Inventory.Enums;

namespace Application.Inventory.Commands;

public sealed class CreateKioskIngredientInventoryCommandHandler(IKioskIngredientInventoryStore inventory)
{
    public async Task<ApiResult<KioskIngredientInventoryResult>> HandleAsync(CreateKioskIngredientInventoryCommand command, CancellationToken cancellationToken = default)
    {
        var kiosk = await inventory.GetKioskForInventoryTopologyAsync(command.KioskId, cancellationToken);
        var ingredient = await inventory.GetIngredientForTopologyAsync(command.IngredientId, cancellationToken);
        if (kiosk is null || ingredient is null) return ApiResult<KioskIngredientInventoryResult>.Fail("Kiosk or ingredient was not found.", 404);
        if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.InventoryConfigure, command.UserContext, kiosk.OrganizationId, kiosk.StoreId, kiosk.Id))
            return ApiResult<KioskIngredientInventoryResult>.Fail("Access denied.", 403);
        try
        {
            return await inventory.ExecuteInTransactionAsync(async ct =>
            {
                var existing = await inventory.GetKioskIngredientInventoryAsync(command.KioskId, command.IngredientId, command.Unit, ct);
                if (existing is not null) return ApiResult<KioskIngredientInventoryResult>.Fail("Kiosk ingredient inventory already exists for this unit.", 409);
                var now = DateTimeOffset.UtcNow;
                var balance = new KioskIngredientInventory
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = kiosk.OrganizationId,
                    StoreId = kiosk.StoreId,
                    KioskId = kiosk.Id,
                    IngredientId = ingredient.Id,
                    CreatedAt = now,
                    CreatedByAccountId = command.UserContext.AccountId
                };
                balance.Configure(command.Unit, command.EstimatedQuantity, command.LowStockThreshold, command.ExpiresAt, command.TrackingMode, now);
                await inventory.AddKioskIngredientInventoryAsync(balance, ct);
                if (command.EstimatedQuantity is > 0)
                {
                    await inventory.AddStockMovementAsync(StockMovement.CreateForKioskInventory(
                        balance.Id, balance.OrganizationId, balance.StoreId, balance.KioskId, balance.IngredientId,
                        "InitialBalance", command.EstimatedQuantity.Value, 0m, command.EstimatedQuantity,
                        balance.Unit, now, "INITIAL_BALANCE", "KioskIngredientInventory", balance.Id,
                        isEstimated: true), ct);
                }
                await inventory.SaveChangesAsync(ct);
                var saved = await inventory.GetKioskIngredientInventoryAsync(balance.Id, ct);
                return ApiResult<KioskIngredientInventoryResult>.Success(KioskIngredientInventoryResultMapper.ToResult(saved!));
            }, cancellationToken);
        }
        catch (DomainRuleException ex) { return ApiResult<KioskIngredientInventoryResult>.Fail(ex.Message, 409); }
    }
}

public sealed class UpdateKioskIngredientInventoryCommandHandler(IKioskIngredientInventoryStore inventory)
{
    public async Task<ApiResult<KioskIngredientInventoryResult>> HandleAsync(UpdateKioskIngredientInventoryCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            return await inventory.ExecuteInTransactionAsync(async token =>
        {
            await inventory.AcquireKioskIngredientInventoryMutationLockAsync(command.InventoryId, token);
            var balance = await inventory.GetKioskIngredientInventoryAsync(command.InventoryId, token);
            if (balance is null || balance.KioskId != command.KioskId) return ApiResult<KioskIngredientInventoryResult>.Fail("Kiosk inventory was not found.", 404);
            if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.InventoryConfigure, command.UserContext, balance.OrganizationId, balance.StoreId, balance.KioskId)) return ApiResult<KioskIngredientInventoryResult>.Fail("Access denied.", 403);
            balance.UpdateMetadata(command.LowStockThreshold, command.ExpiresAt, command.TrackingMode, DateTimeOffset.UtcNow);
            await inventory.SaveChangesAsync(token);
            return ApiResult<KioskIngredientInventoryResult>.Success(KioskIngredientInventoryResultMapper.ToResult(balance));
        }, cancellationToken);
        }
        catch (DomainRuleException ex) { return ApiResult<KioskIngredientInventoryResult>.Fail(ex.Message, 409); }
    }
}

public sealed class AdjustKioskIngredientInventoryCommandHandler(
    IKioskIngredientInventoryStore inventory,
    IRealtimeNotificationPublisher publisher)
{
    public async Task<ApiResult<KioskIngredientInventoryResult>> HandleAsync(AdjustKioskIngredientInventoryCommand command, CancellationToken cancellationToken = default)
    {
        InventoryChangedEvent? notification = null;
        try
        {
            var result = await inventory.ExecuteInTransactionAsync(async token =>
        {
            await inventory.AcquireKioskIngredientInventoryMutationLockAsync(command.InventoryId, token);
            var balance = await inventory.GetKioskIngredientInventoryAsync(command.InventoryId, token);
            if (balance is null || balance.KioskId != command.KioskId) return ApiResult<KioskIngredientInventoryResult>.Fail("Kiosk inventory was not found.", 404);
            if (!ScopeAccessRules.CanAccessScopedRow(ScopeRoleSets.InventoryAdjustManage, command.UserContext, balance.OrganizationId, balance.StoreId, balance.KioskId)) return ApiResult<KioskIngredientInventoryResult>.Fail("Access denied.", 403);
            var before = balance.EstimatedQuantity;
            var now = DateTimeOffset.UtcNow;
            balance.AdjustEstimate(command.EstimatedQuantity, now);
            var delta = command.EstimatedQuantity - (before ?? 0);
            if (delta != 0)
                await inventory.AddStockMovementAsync(StockMovement.CreateForKioskInventory(balance.Id, balance.OrganizationId, balance.StoreId, balance.KioskId, balance.IngredientId, "ManualAdjustment", delta, before, balance.EstimatedQuantity, balance.Unit, now, command.ReasonCode, "KioskIngredientInventory", balance.Id, isEstimated: true), token);
            await inventory.SaveChangesAsync(token);
            notification = new InventoryChangedEvent
            {
                // Cloud balances are valid without a physical dispenser topology.
                DispenserStateId = Guid.Empty,
                KioskId = balance.KioskId,
                OrganizationId = balance.OrganizationId,
                StoreId = balance.StoreId,
                IngredientName = balance.Ingredient.Name,
                EstimatedQuantity = balance.EstimatedQuantity,
                Unit = balance.Unit,
                Status = balance.IsActive ? "Active" : "Inactive",
                UpdatedAt = now,
                Version = 1
            };
            return ApiResult<KioskIngredientInventoryResult>.Success(KioskIngredientInventoryResultMapper.ToResult(balance));
        }, cancellationToken);
            if (result.Succeeded && notification is not null)
            {
                await publisher.PublishInventoryChangedAsync(notification, cancellationToken);
            }

            return result;
        }
        catch (DomainRuleException ex) { return ApiResult<KioskIngredientInventoryResult>.Fail(ex.Message, 409); }
    }
}
