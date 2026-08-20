using Domain.Inventory.Entities;

namespace Application.Inventory.Abstractions;

public interface IInventorySensorObservationStore
{
    Task<T> ExecuteObservationIngestionAsync<T>(
        Guid sourceExecutorId,
        IReadOnlyCollection<Guid> sourceEventIds,
        IReadOnlyCollection<Guid> dispenserStateIds,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);

    Task<IngredientDispenserState?> GetDispenserStateAsync(
        Guid dispenserStateId,
        CancellationToken cancellationToken = default);
    Task AcquireKioskIngredientInventoryMutationLockAsync(Guid inventoryId, CancellationToken cancellationToken = default);

    Task<InventorySensorObservation?> GetObservationBySourceEventAsync(
        Guid sourceExecutorId,
        Guid sourceEventId,
        CancellationToken cancellationToken = default);

    Task<long?> GetLatestAppliedSequenceAsync(
        Guid sourceExecutorId,
        Guid dispenserStateId,
        CancellationToken cancellationToken = default);

    Task AddObservationAsync(
        InventorySensorObservation observation,
        CancellationToken cancellationToken = default);

    Task AddStockMovementAsync(StockMovement movement, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
