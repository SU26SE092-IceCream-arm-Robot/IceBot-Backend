using Application.Operations.Alerts.Automation;
using Domain.Inventory.Entities;
using Domain.Operations.Entities;
using Domain.Operations.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Operations.Persistence;

public sealed class InventoryAlertAutomationStore(IceBotDbContext db) : IInventoryAlertAutomationStore
{
    public async Task<IReadOnlyList<Guid>> ListActiveKioskIngredientInventoryIdsAsync(int maxCount, long scanSlot, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(maxCount, 1, 10_000);
        var query = db.KioskIngredientInventories.WhereNotDeleted().AsNoTracking().Where(balance => balance.IsActive).OrderBy(balance => balance.Id);
        var count = await query.CountAsync(cancellationToken);
        if (count == 0) return [];
        var offset = InventoryAlertScanWindow.CalculateOffset(count, take, scanSlot);
        var result = await query.Skip(offset).Select(balance => balance.Id).Take(take).ToListAsync(cancellationToken);
        if (result.Count == take || result.Count == count) return result;
        result.AddRange(await query.Select(balance => balance.Id).Take(take - result.Count).ToListAsync(cancellationToken));
        return result;
    }

    public Task<KioskIngredientInventory?> GetKioskIngredientInventoryAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.KioskIngredientInventories.WhereNotDeleted().Include(balance => balance.Kiosk).Include(balance => balance.Ingredient)
            .FirstOrDefaultAsync(balance => balance.Id == id && balance.IsActive, cancellationToken);

    public Task<List<InventoryRefillTask>> ListActiveInventoryRefillTasksAsync(Guid kioskIngredientInventoryId, CancellationToken cancellationToken = default) =>
        db.InventoryRefillTasks.WhereNotDeleted().Where(task => task.KioskIngredientInventoryId == kioskIngredientInventoryId &&
            (task.Status == Domain.Inventory.Enums.InventoryRefillTaskStatus.Requested || task.Status == Domain.Inventory.Enums.InventoryRefillTaskStatus.InProgress)).ToListAsync(cancellationToken);
    public Task<List<Alert>> ListActiveBalanceInventoryAlertsAsync(Guid kioskIngredientInventoryId, CancellationToken cancellationToken = default) =>
        db.Alerts.Where(alert => alert.DeletedAt == null && alert.SourceType == "KioskIngredientInventory" &&
            alert.SourceId == kioskIngredientInventoryId && alert.Status != AlertStatus.Resolved && alert.Status != AlertStatus.Suppressed).ToListAsync(cancellationToken);

    public Task AddAlertAsync(Alert alert, CancellationToken cancellationToken = default) =>
        db.Alerts.AddAsync(alert, cancellationToken).AsTask();

    public Task AddInventoryRefillTaskAsync(InventoryRefillTask task, CancellationToken cancellationToken = default) =>
        db.InventoryRefillTasks.AddAsync(task, cancellationToken).AsTask();

    public Task AddInventoryRefillTaskTransitionAsync(InventoryRefillTaskTransition transition, CancellationToken cancellationToken = default) =>
        db.InventoryRefillTaskTransitions.AddAsync(transition, cancellationToken).AsTask();

    public Task AcquireBalanceLockAsync(Guid kioskIngredientInventoryId, CancellationToken cancellationToken = default)
    {
        var lockKey = $"inventory-balance:{kioskIngredientInventoryId:N}";
        return db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0));", cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
