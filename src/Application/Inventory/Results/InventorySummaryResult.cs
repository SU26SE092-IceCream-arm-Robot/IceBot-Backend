namespace Application.Inventory.Results;

public class InventorySummaryResult
{
    public int TotalDispenserCount { get; set; }
    public int LowStockCount { get; set; }
    public int EmptyCount { get; set; }
    public List<InventorySummaryItemDto> Items { get; set; } = new();
}

public class InventorySummaryItemDto
{
    public Guid DispenserStateId { get; set; }
    public Guid? KioskId { get; set; }
    public string KioskCode { get; set; } = null!;
    public string IngredientName { get; set; } = null!;
    public decimal? EstimatedQuantity { get; set; }
    public decimal? Capacity { get; set; }
    public string Unit { get; set; } = "gram";
    public string Status { get; set; } = null!;
    public DateTimeOffset UpdatedAt { get; set; }
}
