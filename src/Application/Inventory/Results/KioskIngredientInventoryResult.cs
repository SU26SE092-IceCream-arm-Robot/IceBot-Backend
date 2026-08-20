using Domain.Inventory.Enums;

namespace Application.Inventory.Results;

public sealed class KioskIngredientInventoryResult
{
    public Guid Id { get; init; }
    public Guid KioskId { get; init; }
    public Guid IngredientId { get; init; }
    public string IngredientCode { get; init; } = null!;
    public string IngredientName { get; init; } = null!;
    public string Unit { get; init; } = null!;
    public decimal? EstimatedQuantity { get; init; }
    public decimal? LowStockThreshold { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public InventoryTrackingMode TrackingMode { get; init; }
    public DateTimeOffset LastMeasuredAt { get; init; }
    public DateTimeOffset? LastSensorReconciledAt { get; init; }
    public bool IsActive { get; init; }
}
