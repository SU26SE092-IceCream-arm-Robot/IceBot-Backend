using Domain.Inventory.Enums;

namespace Application.Inventory.Results;

public sealed class KioskInventoryReadinessResult
{
    public Guid KioskId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid StoreId { get; set; }
    /// <summary>Legacy topology visibility flag. Physical topology is optional.</summary>
    public bool HasConfiguredInventoryTopology { get; set; }
    /// <summary>True once the kiosk has opted into Cloud inventory balance tracking.</summary>
    public bool HasConfiguredInventoryBalance { get; set; }
    public bool IsReady { get; set; }
    public InventoryReadinessStatus OverallStatus { get; set; }
    public IReadOnlyList<InventoryIngredientReadinessResult> Ingredients { get; set; } = [];
    public IReadOnlyList<InventoryOptionGroupReadinessResult> OptionGroups { get; set; } = [];
    public IReadOnlyList<string> Warnings { get; set; } = [];
}

public sealed class InventoryOptionGroupReadinessResult
{
    public Guid ExecutionRouteId { get; set; }
    public string RouteCode { get; set; } = null!;
    public Guid RecipeId { get; set; }
    public long OptionGroupId { get; set; }
    public string OptionGroupCode { get; set; } = null!;
    public bool IsRequired { get; set; }
    public int MinimumSelections { get; set; }
    public int ReadyOptionCount { get; set; }
    public bool IsReady { get; set; }
    public IReadOnlyList<InventoryOptionReadinessResult> Options { get; set; } = [];
}

public sealed class InventoryOptionReadinessResult
{
    public Guid ProductOptionId { get; set; }
    public string OptionCode { get; set; } = null!;
    public bool IsReady { get; set; }
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
    public decimal? RequiredQuantity { get; set; }
    public string? RequiredUnit { get; set; }
    public InventoryReadinessStatus Status { get; set; }
    public IReadOnlyList<Guid> MatchingDispenserStateIds { get; set; } = [];
}
