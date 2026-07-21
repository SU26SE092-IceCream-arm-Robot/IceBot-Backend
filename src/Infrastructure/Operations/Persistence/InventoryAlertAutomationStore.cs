using Application.Operations.Alerts.Automation;
using Domain.Inventory.Entities;
using Domain.Operations.Entities;
using Domain.Operations.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Operations.Persistence;

public sealed class InventoryAlertAutomationStore(IceBotDbContext db) : IInventoryAlertAutomationStore
{
    public async Task<IReadOnlyList<Guid>> ListActiveDispenserStateIdsAsync(
        int maxCount,
        long scanSlot,
        CancellationToken cancellationToken = default) =>
        await ListActiveDispenserStateIdsCoreAsync(maxCount, scanSlot, cancellationToken);

    private async Task<IReadOnlyList<Guid>> ListActiveDispenserStateIdsCoreAsync(
        int maxCount,
        long scanSlot,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(maxCount, 1, 10_000);
        var query = db.IngredientDispenserStates.WhereNotDeleted().AsNoTracking()
            .Where(state => state.IsActive)
            .OrderBy(state => state.Id);
        var count = await query.CountAsync(cancellationToken);
        if (count == 0)
        {
            return [];
        }

        var offset = InventoryAlertScanWindow.CalculateOffset(count, take, scanSlot);
        var result = await query
            .Skip(offset)
            .Select(state => state.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
        if (result.Count == take || result.Count == count)
        {
            return result;
        }

        var remaining = take - result.Count;
        var wrapAround = await query
            .Select(state => state.Id)
            .Take(remaining)
            .ToListAsync(cancellationToken);
        result.AddRange(wrapAround);
        return result;
    }

    public Task<IngredientDispenserState?> GetDispenserStateAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        db.IngredientDispenserStates.WhereNotDeleted()
            .Include(state => state.Kiosk)
            .Include(state => state.Ingredient)
            .FirstOrDefaultAsync(state => state.Id == id && state.IsActive, cancellationToken);

    public Task<List<Alert>> ListActiveInventoryAlertsAsync(
        Guid dispenserStateId,
        CancellationToken cancellationToken = default) =>
        db.Alerts.Where(alert =>
                alert.DeletedAt == null &&
                alert.SourceType == "InventoryDispenserState" &&
                alert.SourceId == dispenserStateId &&
                alert.Status != AlertStatus.Resolved &&
                alert.Status != AlertStatus.Suppressed)
            .ToListAsync(cancellationToken);

    public Task<bool> MaintenanceTicketExistsForAlertAsync(
        Guid alertId,
        CancellationToken cancellationToken = default) =>
        db.MaintenanceTickets.AnyAsync(ticket => ticket.AlertId == alertId, cancellationToken);

    public Task AddAlertAsync(Alert alert, CancellationToken cancellationToken = default) =>
        db.Alerts.AddAsync(alert, cancellationToken).AsTask();

    public Task AddMaintenanceTicketAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default) =>
        db.MaintenanceTickets.AddAsync(ticket, cancellationToken).AsTask();

    public Task AcquireLockAsync(Guid dispenserStateId, CancellationToken cancellationToken = default)
    {
        var lockKey = $"inventory-alert:{dispenserStateId:N}";
        return db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0));",
            cancellationToken);
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
