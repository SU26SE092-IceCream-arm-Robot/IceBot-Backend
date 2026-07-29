using Domain.Inventory.Entities;
using Domain.Inventory.Enums;

namespace Application.Inventory.Support;

internal static class InventoryTopologyAuditFactory
{
    public static InventoryTopologyChangeRecord Create(
        IngredientDispenserState state,
        InventoryTopologyChangeType changeType,
        string reason,
        Guid? actorId,
        DateTimeOffset occurredAt,
        bool? beforeIsActive = null,
        decimal? beforeCapacity = null,
        string? beforeUnit = null) => new()
    {
        DispenserStateId = state.Id,
        KioskId = state.KioskId ?? Guid.Empty,
        DeviceId = state.DeviceId,
        IngredientId = state.IngredientId,
        ContainerCode = state.ContainerCode,
        ChangeType = changeType,
        BeforeIsActive = beforeIsActive,
        AfterIsActive = changeType == InventoryTopologyChangeType.Deleted ? null : state.IsActive,
        BeforeCapacityQuantity = beforeCapacity,
        AfterCapacityQuantity = changeType == InventoryTopologyChangeType.Deleted ? null : state.CapacityQuantity,
        BeforeUnit = beforeUnit,
        AfterUnit = changeType == InventoryTopologyChangeType.Deleted ? null : state.Unit,
        Reason = reason.Trim(),
        CreatedAt = occurredAt,
        CreatedByAccountId = actorId
    };
}
