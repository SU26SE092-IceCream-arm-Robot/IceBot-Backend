using Domain.Inventory.Entities;
using Domain.Operations.Entities;

namespace Application.Operations.Alerts.Automation;

public interface IInventoryAlertAutomationStore
{
    Task<IReadOnlyList<Guid>> ListActiveDispenserStateIdsAsync(
        int maxCount,
        long scanSlot,
        CancellationToken cancellationToken = default);
    Task<IngredientDispenserState?> GetDispenserStateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Alert>> ListActiveInventoryAlertsAsync(Guid dispenserStateId, CancellationToken cancellationToken = default);
    Task<bool> MaintenanceTicketExistsForAlertAsync(Guid alertId, CancellationToken cancellationToken = default);
    Task AddAlertAsync(Alert alert, CancellationToken cancellationToken = default);
    Task AddMaintenanceTicketAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default);
    Task AcquireLockAsync(Guid dispenserStateId, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
}
