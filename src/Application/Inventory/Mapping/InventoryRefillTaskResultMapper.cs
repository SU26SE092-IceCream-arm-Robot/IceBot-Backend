using Application.Inventory.Results;
using Domain.Inventory.Entities;

namespace Application.Inventory.Mapping;

internal static class InventoryRefillTaskResultMapper
{
    public static InventoryRefillTaskResult ToResult(InventoryRefillTask task) => new()
    {
        Id = task.Id,
        KioskId = task.KioskId,
        KioskIngredientInventoryId = task.KioskIngredientInventoryId,
        IngredientDispenserStateId = task.IngredientDispenserStateId,
        SourceAlertId = task.SourceAlertId,
        RequestSource = task.RequestSource,
        Status = task.Status,
        RequestedQuantity = task.RequestedQuantity,
        ActualQuantity = task.ActualQuantity,
        Unit = task.Unit,
        ReasonCode = task.ReasonCode,
        Notes = task.Notes,
        ExternalLotReference = task.ExternalLotReference,
        RequestedAt = task.RequestedAt,
        StartedAt = task.StartedAt,
        CompletedAt = task.CompletedAt,
        CancelledAt = task.CancelledAt,
        CancellationReason = task.CancellationReason,
        Version = task.Version
    };
}
