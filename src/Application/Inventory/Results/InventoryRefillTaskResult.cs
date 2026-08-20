using Domain.Inventory.Enums;

namespace Application.Inventory.Results;

public sealed class InventoryRefillTaskResult
{
    public Guid Id { get; init; }
    public Guid KioskId { get; init; }
    public Guid KioskIngredientInventoryId { get; init; }
    public Guid? IngredientDispenserStateId { get; init; }
    public Guid? SourceAlertId { get; init; }
    public InventoryRefillRequestSource RequestSource { get; init; }
    public InventoryRefillTaskStatus Status { get; init; }
    public decimal? RequestedQuantity { get; init; }
    public decimal? ActualQuantity { get; init; }
    public string Unit { get; init; } = null!;
    public string? ReasonCode { get; init; }
    public string? Notes { get; init; }
    public string? ExternalLotReference { get; init; }
    public DateTimeOffset RequestedAt { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
    public long Version { get; init; }
}
