using Application.Inventory.Abstractions;
using Application.Inventory.Support;
using Domain.Inventory.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Inventory.Persistence;

public sealed class InventorySensorObservationStore(IceBotDbContext dbContext) : IInventorySensorObservationStore
{
    public async Task<T> ExecuteObservationIngestionAsync<T>(
        Guid sourceExecutorId,
        IReadOnlyCollection<Guid> sourceEventIds,
        IReadOnlyCollection<Guid> dispenserStateIds,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        foreach (var sourceEventId in sourceEventIds.Distinct().OrderBy(id => id))
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({$"inventory-sensor-observation:{sourceExecutorId:D}:{sourceEventId:D}"}, 0));",
                cancellationToken);
        }

        foreach (var dispenserStateId in dispenserStateIds.Distinct().OrderBy(id => id))
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({InventoryConcurrency.DispenserLockKey(dispenserStateId)}, 0));",
                cancellationToken);
        }

        var result = await action(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public Task<IngredientDispenserState?> GetDispenserStateAsync(
        Guid dispenserStateId,
        CancellationToken cancellationToken = default) =>
        dbContext.IngredientDispenserStates.IgnoreQueryFilters()
            .Include(state => state.Kiosk)
            .Include(state => state.Ingredient)
            .Include(state => state.KioskIngredientInventory)
            .FirstOrDefaultAsync(state => state.Id == dispenserStateId && state.DeletedAt == null, cancellationToken);

    public Task AcquireKioskIngredientInventoryMutationLockAsync(Guid inventoryId, CancellationToken cancellationToken = default) =>
        dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"inventory-balance:{inventoryId:N}"}, 0));",
            cancellationToken);

    public Task<InventorySensorObservation?> GetObservationBySourceEventAsync(
        Guid sourceExecutorId,
        Guid sourceEventId,
        CancellationToken cancellationToken = default) =>
        dbContext.InventorySensorObservations.FirstOrDefaultAsync(
            observation => observation.SourceExecutorId == sourceExecutorId && observation.SourceEventId == sourceEventId,
            cancellationToken);

    public Task<long?> GetLatestAppliedSequenceAsync(
        Guid sourceExecutorId,
        Guid dispenserStateId,
        CancellationToken cancellationToken = default) =>
        dbContext.InventorySensorObservations
            .Where(observation => observation.SourceExecutorId == sourceExecutorId &&
                observation.IngredientDispenserStateId == dispenserStateId &&
                observation.Disposition == Domain.Inventory.Enums.InventorySensorObservationDisposition.Applied)
            .Select(observation => (long?)observation.ObservationSequence)
            .MaxAsync(cancellationToken);

    public Task AddObservationAsync(
        InventorySensorObservation observation,
        CancellationToken cancellationToken = default) =>
        dbContext.InventorySensorObservations.AddAsync(observation, cancellationToken).AsTask();

    public Task AddStockMovementAsync(StockMovement movement, CancellationToken cancellationToken = default) =>
        dbContext.StockMovements.AddAsync(movement, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
