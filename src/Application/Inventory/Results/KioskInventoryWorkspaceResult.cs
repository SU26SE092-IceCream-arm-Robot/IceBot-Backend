using Domain.Inventory.Enums;

namespace Application.Inventory.Results;

public sealed class KioskInventoryWorkspaceResult
{
    public required KioskInventoryWorkspaceSummaryResult Summary { get; init; }
    public required IReadOnlyList<KioskInventoryWorkspaceInventoryResult> Inventories { get; init; }
    public required IReadOnlyList<InventoryRefillTaskResult> ActiveRefillTasks { get; init; }
    public required KioskInventoryWorkspaceActionsResult AvailableActions { get; init; }
}

public sealed class KioskInventoryWorkspaceSummaryResult
{
    public int InventoryCount { get; init; }
    public int LowInventoryCount { get; init; }
    public int EmptyInventoryCount { get; init; }
    public int ExpiredInventoryCount { get; init; }
    public int ActiveRefillTaskCount { get; init; }
}

public sealed class KioskInventoryWorkspaceInventoryResult
{
    public required KioskIngredientInventoryResult Inventory { get; init; }
    public required KioskInventoryLevelStatus InventoryStatus { get; init; }
    public InventoryRefillTaskResult? ActiveRefillTask { get; init; }
}

public enum KioskInventoryLevelStatus
{
    InStock = 1,
    Low = 2,
    Empty = 3,
    Expired = 4,
    Unknown = 5
}

public sealed class KioskInventoryWorkspaceActionsResult
{
    public bool CanManageRefill { get; init; }
    public bool CanAdjustInventory { get; init; }
    public bool CanConfigureInventory { get; init; }
    public IReadOnlyList<InventoryRefillTaskStatus> StartableRefillTaskStatuses { get; init; } = [];
    public IReadOnlyList<InventoryRefillTaskStatus> CompletableRefillTaskStatuses { get; init; } = [];
}
