using Application.Inventory.Results;
using Domain.Inventory.Entities;
using Domain.Catalog.Entities;
using Domain.Devices.Catalog;
using Domain.Tenants.Entities;
using Domain.Identity.Entities;
using Domain.Operations.Entities;

namespace Application.Inventory.Abstractions;

public interface IInventoryStore
{
    Task<InventorySummaryResult> GetInventorySummaryAsync(
        Guid? kioskId,
        Guid? storeId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default);

    Task<IngredientDispenserState?> GetDispenserStateByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<KioskIngredientInventory?> GetKioskIngredientInventoryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<KioskIngredientInventory?> GetKioskIngredientInventoryAsync(Guid kioskId, Guid ingredientId, string unit, CancellationToken cancellationToken = default);
    Task<List<KioskIngredientInventory>> ListKioskIngredientInventoriesAsync(Guid kioskId, CancellationToken cancellationToken = default);
    Task<InventoryRefillTask?> GetInventoryRefillTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<InventoryRefillTask?> GetInventoryRefillTaskByRequestKeyAsync(Guid kioskId, string requestIdempotencyKey, CancellationToken cancellationToken = default);
    Task<int> CountInventoryRefillTasksAsync(
        Guid kioskId,
        Domain.Inventory.Enums.InventoryRefillTaskStatus? status,
        DateTimeOffset? requestedFrom,
        DateTimeOffset? requestedTo,
        CancellationToken cancellationToken = default);
    Task<List<InventoryRefillTask>> ListInventoryRefillTasksAsync(
        Guid kioskId,
        Domain.Inventory.Enums.InventoryRefillTaskStatus? status,
        DateTimeOffset? requestedFrom,
        DateTimeOffset? requestedTo,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<List<InventoryRefillTask>> ListActiveInventoryRefillTasksAsync(
        Guid kioskId,
        int take,
        CancellationToken cancellationToken = default);
    Task<InventoryRefillTask?> GetActiveInventoryRefillTaskAsync(Guid kioskIngredientInventoryId, CancellationToken cancellationToken = default);
    Task<List<IngredientDispenserState>> ListBoundDispenserStatesForMutationAsync(Guid kioskIngredientInventoryId, CancellationToken cancellationToken = default);
    Task<List<IngredientDispenserState>> ListBoundDispenserStatesAsync(Guid kioskIngredientInventoryId, CancellationToken cancellationToken = default);
    Task<Device?> GetDeviceForTopologyAsync(Guid kioskId, Guid deviceId, CancellationToken cancellationToken = default);
    Task<Ingredient?> GetIngredientForTopologyAsync(Guid ingredientId, CancellationToken cancellationToken = default);
    Task<Kiosk?> GetKioskForInventoryTopologyAsync(Guid kioskId, CancellationToken cancellationToken = default);
    Task<List<Device>> ListDevicesForInventoryTopologyAsync(Guid kioskId, CancellationToken cancellationToken = default);
    Task<List<IngredientDispenserState>> ListStatesForInventoryTopologyAsync(Guid kioskId, CancellationToken cancellationToken = default);
    Task<List<Kiosk>> ListKiosksForInventoryReadinessAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<List<RecipeItem>> ListRequiredRecipeItemsAsync(IReadOnlyCollection<Guid> recipeIds, CancellationToken cancellationToken = default);
    Task<List<ProductOption>> ListSupportedProductOptionsAsync(
        IReadOnlyCollection<Guid> productIds,
        IReadOnlyCollection<string> optionCodes,
        CancellationToken cancellationToken = default);
    Task<bool> DispenserIdentityExistsAsync(Guid deviceId, string containerCode, Guid? excludedId = null, CancellationToken cancellationToken = default);
    Task<bool> HasStockMovementsAsync(Guid dispenserStateId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveExecutionAsync(Guid kioskId, CancellationToken cancellationToken = default);
    Task AcquireDeviceTopologyMutationLocksAsync(IEnumerable<Guid> deviceIds, CancellationToken cancellationToken = default);
    Task AcquireDispenserMutationLockAsync(Guid dispenserStateId, CancellationToken cancellationToken = default);
    Task AcquireKioskIngredientInventoryMutationLockAsync(Guid kioskIngredientInventoryId, CancellationToken cancellationToken = default);
    Task AcquireInventoryRefillTaskMutationLockAsync(Guid inventoryRefillTaskId, CancellationToken cancellationToken = default);
    Task AddKioskIngredientInventoryAsync(KioskIngredientInventory inventory, CancellationToken cancellationToken = default);
    Task AddInventoryRefillTaskAsync(InventoryRefillTask task, CancellationToken cancellationToken = default);
    Task AddInventoryRefillTaskTransitionAsync(InventoryRefillTaskTransition transition, CancellationToken cancellationToken = default);
    Task<InventoryRefillTaskTransition?> GetInventoryRefillTaskTransitionByRequestKeyAsync(Guid taskId, string requestIdempotencyKey, CancellationToken cancellationToken = default);
    Task<Alert?> GetAlertByIdAsync(Guid alertId, CancellationToken cancellationToken = default);
    Task AcquireAlertMutationLockAsync(Guid alertId, CancellationToken cancellationToken = default);
    Task AddInventoryReconciliationCaseAsync(InventoryReconciliationCase reconciliationCase, CancellationToken cancellationToken = default);
    Task AddDispenserStateAsync(IngredientDispenserState state, CancellationToken cancellationToken = default);
    void RemoveDispenserState(IngredientDispenserState state);
    Task AddTopologyRebindRecordAsync(InventoryTopologyRebindRecord record, CancellationToken cancellationToken = default);
    Task<List<InventoryTopologyRebindRecord>> ListTopologyRebindRecordsAsync(Guid dispenserStateId, int? take = null, CancellationToken cancellationToken = default);
    Task<List<IngredientDispenserState>> ListActiveDispenserStatesByDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default);
    Task<List<Guid>> ListActiveDispenserStateIdsByDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default);
    Task AddTopologyChangeRecordAsync(InventoryTopologyChangeRecord record, CancellationToken cancellationToken = default);
    Task<List<InventoryTopologyChangeRecord>> ListTopologyChangeRecordsAsync(Guid dispenserStateId, int take, CancellationToken cancellationToken = default);
    Task<int> CountTopologyChangeRecordsAsync(Guid dispenserStateId, CancellationToken cancellationToken = default);
    Task<List<StockMovement>> ListStockMovementsForDispenserAsync(Guid dispenserStateId, int take, CancellationToken cancellationToken = default);
    Task<List<InventorySensorObservation>> ListSensorObservationsForDispenserAsync(Guid dispenserStateId, int take, CancellationToken cancellationToken = default);
    Task<int> CountStockMovementsForDispenserAsync(Guid dispenserStateId, CancellationToken cancellationToken = default);
    Task<int> CountSensorObservationsForDispenserAsync(Guid dispenserStateId, CancellationToken cancellationToken = default);
    Task<int> CountTopologyRebindRecordsAsync(Guid dispenserStateId, CancellationToken cancellationToken = default);
    Task<List<Account>> ListAccountsForInventoryHistoryAsync(IReadOnlyCollection<Guid> accountIds, CancellationToken cancellationToken = default);

    Task AddStockMovementAsync(StockMovement movement, CancellationToken cancellationToken = default);

    Task<int> CountDispenserStatesAsync(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool? isActive,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default);

    Task<List<IngredientDispenserState>> ListDispenserStatesAsync(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool? isActive,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountStockMovementsAsync(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default);

    Task<List<StockMovement>> ListStockMovementsAsync(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken = default);

    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
}
