using Domain.Inventory.Entities;
using Domain.Operations.Entities;

namespace Application.Operations.Alerts.Automation;

public interface IInventoryAlertAutomationStore
{
    Task<IReadOnlyList<Guid>> ListActiveKioskIngredientInventoryIdsAsync(int maxCount, long scanSlot, CancellationToken cancellationToken = default);
    Task<KioskIngredientInventory?> GetKioskIngredientInventoryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<InventoryRefillTask>> ListActiveInventoryRefillTasksAsync(Guid kioskIngredientInventoryId, CancellationToken cancellationToken = default);
    Task<List<Alert>> ListActiveBalanceInventoryAlertsAsync(Guid kioskIngredientInventoryId, CancellationToken cancellationToken = default);
    Task AddAlertAsync(Alert alert, CancellationToken cancellationToken = default);
    Task AddInventoryRefillTaskAsync(InventoryRefillTask task, CancellationToken cancellationToken = default);
    Task AddInventoryRefillTaskTransitionAsync(InventoryRefillTaskTransition transition, CancellationToken cancellationToken = default);
    Task AcquireBalanceLockAsync(Guid kioskIngredientInventoryId, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
}
