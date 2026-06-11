using Domain.Inventory.Enums;
using System;

namespace Application.Inventory.Results;

public sealed class DispenserStateResult
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public string DeviceCode { get; set; } = null!;
    public Guid? KioskId { get; set; }
    public string? KioskName { get; set; }
    public Guid IngredientId { get; set; }
    public string IngredientName { get; set; } = null!;
    public string IngredientCode { get; set; } = null!;
    public string ContainerCode { get; set; } = null!;
    public IngredientLevelStatus CurrentLevelStatus { get; set; }
    public decimal? EstimatedQuantity { get; set; }
    public decimal? CapacityQuantity { get; set; }
    public string Unit { get; set; } = "gram";
    public DateTimeOffset LastMeasuredAt { get; set; }
    public DateTimeOffset? LastRefilledAt { get; set; }
}
