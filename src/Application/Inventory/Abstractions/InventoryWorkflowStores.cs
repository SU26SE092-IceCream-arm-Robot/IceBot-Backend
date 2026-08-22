using Domain.Catalog.Entities;
using Domain.Devices.Catalog;
using Domain.Identity.Entities;
using Domain.Inventory.Entities;
using Domain.Inventory.Enums;
using Domain.Operations.Entities;
using Domain.Tenants.Entities;

namespace Application.Inventory.Abstractions;

public interface IInventoryTransactionStore
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);
}

public interface IKioskIngredientInventoryStore : IInventoryTransactionStore
{
    Task<Kiosk?> GetKioskForInventoryTopologyAsync(Guid kioskId, CancellationToken cancellationToken = default);

    Task<Ingredient?> GetIngredientForTopologyAsync(Guid ingredientId, CancellationToken cancellationToken = default);

    Task<KioskIngredientInventory?> GetKioskIngredientInventoryAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<KioskIngredientInventory?> GetKioskIngredientInventoryAsync(
        Guid kioskId,
        Guid ingredientId,
        string unit,
        CancellationToken cancellationToken = default);

    Task<List<KioskIngredientInventory>> ListKioskIngredientInventoriesAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default);

    Task AcquireKioskIngredientInventoryMutationLockAsync(
        Guid kioskIngredientInventoryId,
        CancellationToken cancellationToken = default);

    Task AddKioskIngredientInventoryAsync(
        KioskIngredientInventory inventory,
        CancellationToken cancellationToken = default);

    Task AddStockMovementAsync(StockMovement movement, CancellationToken cancellationToken = default);
}

public interface IInventoryRefillTaskReadStore
{
    Task<Kiosk?> GetKioskForInventoryTopologyAsync(Guid kioskId, CancellationToken cancellationToken = default);

    Task<InventoryRefillTask?> GetInventoryRefillTaskAsync(Guid taskId, CancellationToken cancellationToken = default);

    Task<int> CountInventoryRefillTasksAsync(
        Guid kioskId,
        InventoryRefillTaskStatus? status,
        DateTimeOffset? requestedFrom,
        DateTimeOffset? requestedTo,
        CancellationToken cancellationToken = default);

    Task<List<InventoryRefillTask>> ListInventoryRefillTasksAsync(
        Guid kioskId,
        InventoryRefillTaskStatus? status,
        DateTimeOffset? requestedFrom,
        DateTimeOffset? requestedTo,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public interface IInventoryRefillTaskStore : IInventoryRefillTaskReadStore, IInventoryTransactionStore
{
    Task<InventoryRefillTask?> GetInventoryRefillTaskByRequestKeyAsync(
        Guid kioskId,
        string requestIdempotencyKey,
        CancellationToken cancellationToken = default);

    Task<InventoryRefillTask?> GetActiveInventoryRefillTaskAsync(
        Guid kioskIngredientInventoryId,
        CancellationToken cancellationToken = default);

    Task<KioskIngredientInventory?> GetKioskIngredientInventoryAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AcquireKioskIngredientInventoryMutationLockAsync(
        Guid kioskIngredientInventoryId,
        CancellationToken cancellationToken = default);

    Task AcquireInventoryRefillTaskMutationLockAsync(
        Guid inventoryRefillTaskId,
        CancellationToken cancellationToken = default);

    Task AddInventoryRefillTaskAsync(InventoryRefillTask task, CancellationToken cancellationToken = default);

    Task<InventoryRefillTaskTransition?> GetInventoryRefillTaskTransitionByRequestKeyAsync(
        Guid taskId,
        string requestIdempotencyKey,
        CancellationToken cancellationToken = default);

    Task AddInventoryRefillTaskTransitionAsync(
        InventoryRefillTaskTransition transition,
        CancellationToken cancellationToken = default);

    Task AddStockMovementAsync(StockMovement movement, CancellationToken cancellationToken = default);

    Task<IngredientDispenserState?> GetDispenserStateByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<List<IngredientDispenserState>> ListBoundDispenserStatesForMutationAsync(
        Guid kioskIngredientInventoryId,
        CancellationToken cancellationToken = default);

    Task<Alert?> GetAlertByIdAsync(Guid alertId, CancellationToken cancellationToken = default);

    Task AcquireAlertMutationLockAsync(Guid alertId, CancellationToken cancellationToken = default);
}

public interface IInventoryWorkspaceStore
{
    Task<Kiosk?> GetKioskForInventoryTopologyAsync(Guid kioskId, CancellationToken cancellationToken = default);

    Task<List<KioskIngredientInventory>> ListKioskIngredientInventoriesAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default);

    Task<List<InventoryRefillTask>> ListActiveInventoryRefillTasksAsync(
        Guid kioskId,
        int take,
        CancellationToken cancellationToken = default);
}
