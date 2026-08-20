using Application.Inventory.Results;
using Domain.Inventory.Entities;

namespace Application.Inventory.Mapping;

internal static class KioskIngredientInventoryResultMapper
{
    public static KioskIngredientInventoryResult ToResult(KioskIngredientInventory inventory) => new()
    {
        Id = inventory.Id,
        KioskId = inventory.KioskId,
        IngredientId = inventory.IngredientId,
        IngredientCode = inventory.Ingredient.Code,
        IngredientName = inventory.Ingredient.Name,
        Unit = inventory.Unit,
        EstimatedQuantity = inventory.EstimatedQuantity,
        LowStockThreshold = inventory.LowStockThreshold,
        ExpiresAt = inventory.ExpiresAt,
        TrackingMode = inventory.TrackingMode,
        LastMeasuredAt = inventory.LastMeasuredAt,
        LastSensorReconciledAt = inventory.LastSensorReconciledAt,
        IsActive = inventory.IsActive
    };
}
