using Application.Inventory.Results;
using Domain.Inventory.Entities;
using Application.Inventory.Support;

namespace Application.Inventory.Mapping;

internal static class DispenserStateResultMapper
{
    public static DispenserStateResult ToResult(IngredientDispenserState state)
    {
        return new DispenserStateResult
        {
            Id = state.Id,
            DeviceId = state.DeviceId,
            DeviceCode = state.Device.Code,
            KioskId = state.KioskId,
            KioskName = state.Kiosk?.Name,
            IngredientId = state.IngredientId,
            IngredientName = state.Ingredient.Name,
            IngredientCode = state.Ingredient.Code,
            ContainerCode = state.ContainerCode,
            CurrentLevelStatus = state.CurrentLevelStatus,
            EstimatedQuantity = state.EstimatedQuantity,
            CapacityQuantity = state.CapacityQuantity,
            Unit = state.Unit,
            TrackingMode = state.TrackingMode,
            LastMeasuredAt = state.LastMeasuredAt,
            LastSensorObservedAt = state.LastSensorObservedAt,
            LastRefilledAt = state.LastRefilledAt,
            IsActive = state.IsActive,
            LevelToQuantityProfile = DispenserLevelQuantityProfileContract.Deserialize(state.LevelToQuantityProfileJson)
        };
    }
}
