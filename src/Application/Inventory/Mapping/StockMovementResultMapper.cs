using Application.Inventory.Results;
using Domain.Inventory.Entities;

namespace Application.Inventory.Mapping;

internal static class StockMovementResultMapper
{
    public static StockMovementResult ToResult(StockMovement m)
    {
        return new StockMovementResult
        {
            Id = m.Id,
            IngredientDispenserStateId = m.IngredientDispenserStateId,
            ContainerCode = m.IngredientDispenserState.ContainerCode,
            KioskId = m.KioskId,
            KioskName = m.Kiosk?.Name,
            IngredientId = m.IngredientId,
            IngredientName = m.Ingredient?.Name,
            MovementType = m.MovementType,
            Quantity = m.Quantity,
            BalanceBefore = m.BalanceBefore,
            BalanceAfter = m.BalanceAfter,
            Unit = m.Unit,
            ReasonCode = m.ReasonCode,
            Notes = m.Notes,
            OccurredAt = m.OccurredAt,
            ActorAccountId = m.CreatedByAccountId,
            ActorName = m.CreatedByAccount?.FullName,
            ActorEmail = m.CreatedByAccount?.Email
        };
    }
}
