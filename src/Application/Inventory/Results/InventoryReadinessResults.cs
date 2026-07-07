using Domain.Inventory.Enums;

namespace Application.Inventory.Results;

public sealed class KioskInventoryReadinessResult
{
    public Guid KioskId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid StoreId { get; set; }
    public bool IsReady { get; set; }
    public InventoryReadinessStatus OverallStatus { get; set; }
    public IReadOnlyList<InventoryIngredientReadinessResult> Ingredients { get; set; } = [];
}

public sealed class InventoryIngredientReadinessResult
{
    public Guid ExecutionRouteId { get; set; }
    public string RouteCode { get; set; } = null!;
    public Guid RecipeId { get; set; }
    public Guid IngredientId { get; set; }
    public string IngredientCode { get; set; } = null!;
    public string IngredientName { get; set; } = null!;
    public InventoryReadinessStatus Status { get; set; }
    public IReadOnlyList<Guid> MatchingDispenserStateIds { get; set; } = [];
}
