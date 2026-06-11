using System;

namespace Application.Inventory.Results;

public sealed class StockMovementResult
{
    public Guid Id { get; set; }
    public Guid IngredientDispenserStateId { get; set; }
    public string ContainerCode { get; set; } = null!;
    public Guid? KioskId { get; set; }
    public string? KioskName { get; set; }
    public Guid? IngredientId { get; set; }
    public string? IngredientName { get; set; }
    public string MovementType { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal? BalanceAfter { get; set; }
    public string Unit { get; set; } = "gram";
    public string? ReasonCode { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
