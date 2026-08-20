using Domain.Inventory.Enums;

namespace WebAPI.Controllers.Inventory;

public sealed class CreateKioskIngredientInventoryRequest
{
    public Guid IngredientId { get; init; }
    public string Unit { get; init; } = "gram";
    public decimal? EstimatedQuantity { get; init; }
    public decimal? LowStockThreshold { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public InventoryTrackingMode TrackingMode { get; init; } = InventoryTrackingMode.ManualEstimate;
}

public sealed class UpdateKioskIngredientInventoryRequest
{
    public decimal? LowStockThreshold { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public InventoryTrackingMode TrackingMode { get; init; } = InventoryTrackingMode.ManualEstimate;
}

public sealed class AdjustKioskIngredientInventoryRequest
{
    public decimal EstimatedQuantity { get; init; }
    public string? ReasonCode { get; init; }
}

public sealed class RequestInventoryRefillTaskRequest
{
    public decimal? RequestedQuantity { get; init; }
    public Guid? IngredientDispenserStateId { get; init; }
    public string? ReasonCode { get; init; }
    public string? Notes { get; init; }
}

public sealed class CompleteInventoryRefillTaskRequest
{
    public decimal ActualQuantity { get; init; }
    public Guid? IngredientDispenserStateId { get; init; }
    public string? ReasonCode { get; init; }
    public string? Notes { get; init; }
    public string? ExternalLotReference { get; init; }
}

public sealed class CancelInventoryRefillTaskRequest
{
    public string Reason { get; init; } = string.Empty;
}
