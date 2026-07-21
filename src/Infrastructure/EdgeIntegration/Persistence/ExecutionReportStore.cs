using Domain.Sync.Ingestion;
using Domain.Devices.ExecutionEndpoints;
using Application.EdgeIntegration.Abstractions;
using Domain.Devices.Catalog;
using Domain.Inventory.Entities;
using Domain.Orders.Entities;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionExecution.Projections;
using Domain.Sync.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Application.Orders.Support;
using Application.EdgeIntegration.CommandDelivery.Services;
using Application.Inventory.Support;
using System.Text.Json;

namespace Infrastructure.EdgeIntegration.Persistence;

public sealed class ExecutionReportStore :
    IExecutionReportUnitOfWork
{
    private readonly IceBotDbContext _dbContext;

    public ExecutionReportStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> ExecuteReportIngestionAsync<T>(
        Guid sourceExecutorId,
        Guid sourceEventId,
        Guid commandId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({EdgeCommandConcurrency.CommandLockKey(commandId)}, 0));",
            cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"execution-report:{sourceExecutorId:D}:{sourceEventId:D}"}, 0));",
            cancellationToken);
        var result = await action(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public Task<KioskExecutionEndpoint?> GetEndpointForReportAuthAsync(
        Guid endpointId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskExecutionEndpoints.WhereNotDeleted()
            .Include(endpoint => endpoint.CredentialBinding)
            .FirstOrDefaultAsync(
                endpoint => endpoint.Id == endpointId && endpoint.Kiosk.DeletedAt == null,
                cancellationToken);
    }

    public Task<SyncEventInbox?> GetSyncEventByEventIdAsync(
        Guid sourceExecutorId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SyncEventInbox
            .FirstOrDefaultAsync(
                syncEvent => syncEvent.SourceNodeId == sourceExecutorId && syncEvent.EventId == eventId,
                cancellationToken);
    }

    public async Task AcquireStockMovementLocksAsync(
        IEnumerable<Guid> sourceEventIds,
        CancellationToken cancellationToken = default)
    {
        foreach (var sourceEventId in sourceEventIds.Distinct().OrderBy(id => id))
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({$"stock-movement:{sourceEventId:D}"}, 0));",
                cancellationToken);
        }
    }

    public Task AddSyncEventAsync(
        SyncEventInbox syncEvent,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SyncEventInbox.AddAsync(syncEvent, cancellationToken).AsTask();
    }

    public Task<EdgeCommand?> GetCommandAsync(
        Guid commandId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.EdgeCommands
            .FirstOrDefaultAsync(command => command.Id == commandId, cancellationToken);
    }

    public Task<KioskConfigurationDeployment?> GetFullEdgeDeploymentAsync(
        Guid deploymentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskConfigurationDeployments
            .Include(deployment => deployment.KioskExecutionEndpoint)
            .FirstOrDefaultAsync(deployment => deployment.Id == deploymentId, cancellationToken);
    }

    public Task<ControllerArtifactSetDeployment?> GetControllerArtifactSetDeploymentAsync(
        Guid deploymentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ControllerArtifactSetDeployments
            .Include(deployment => deployment.KioskExecutionEndpoint)
            .FirstOrDefaultAsync(deployment => deployment.Id == deploymentId, cancellationToken);
    }

    public Task<ProductionExecutionRecord?> GetProductionExecutionRecordAsync(
        Guid sourceCommandId,
        Guid sourceProductionJobId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductionExecutionRecords
            .FirstOrDefaultAsync(record =>
                record.SourceCommandId == sourceCommandId &&
                record.SourceProductionJobId == sourceProductionJobId,
                cancellationToken);
    }

    public Task AddProductionExecutionRecordAsync(
        ProductionExecutionRecord record,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductionExecutionRecords.AddAsync(record, cancellationToken).AsTask();
    }

    public Task<OrderExecutionRecord?> GetOrderExecutionRecordAsync(
        Guid sourceCommandId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.OrderExecutionRecords
            .FirstOrDefaultAsync(record => record.SourceCommandId == sourceCommandId, cancellationToken);
    }

    public Task AddOrderExecutionRecordAsync(
        OrderExecutionRecord record,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.OrderExecutionRecords.AddAsync(record, cancellationToken).AsTask();
    }

    public Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .Include(order => order.OrderItems)
            .FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);
    }

    public Task AddOrderStatusHistoryAsync(
        OrderStatusHistory history,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.OrderStatusHistories.AddAsync(history, cancellationToken).AsTask();
    }

    public Task<IngredientDispenserState?> GetDispenserStateAsync(
        Guid dispenserStateId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.IngredientDispenserStates
            .Include(state => state.Kiosk)
            .Include(state => state.Ingredient)
            .FirstOrDefaultAsync(state => state.Id == dispenserStateId, cancellationToken);
    }

    public async Task AcquireDispenserMutationLocksAsync(
        IEnumerable<Guid> dispenserStateIds,
        CancellationToken cancellationToken = default)
    {
        foreach (var dispenserStateId in dispenserStateIds.Distinct().OrderBy(id => id))
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({InventoryConcurrency.DispenserLockKey(dispenserStateId)}, 0));",
                cancellationToken);
        }
    }

    public Task AcquireOrderWorkflowLockAsync(
        Guid orderId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({OrderWorkflowConcurrency.OrderLockKey(orderId)}, 0));",
            cancellationToken);

    public Task AddOrderItemStatusHistoryAsync(
        OrderItemStatusHistory history,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.OrderItemStatusHistories.AddAsync(history, cancellationToken).AsTask();
    }

    public async Task<bool> IsIngredientExpectedForOrderItemAsync(
        Guid orderId,
        Guid orderItemId,
        Guid ingredientId,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.OrderItems.AsNoTracking()
            .Where(candidate => candidate.Id == orderItemId && candidate.OrderId == orderId)
            .Select(candidate => new { candidate.RecipeId, candidate.RecipeSnapshotSchemaVersion, candidate.RecipeSnapshotJson })
            .FirstOrDefaultAsync(cancellationToken);
        if (item is null) return false;

        if (item.RecipeSnapshotSchemaVersion >= 2)
        {
            if (string.IsNullOrWhiteSpace(item.RecipeSnapshotJson)) return false;
            try
            {
                using var document = JsonDocument.Parse(item.RecipeSnapshotJson);
                var recipeIngredientExists = document.RootElement.TryGetProperty("Ingredients", out var ingredients) &&
                    ingredients.ValueKind == JsonValueKind.Array &&
                    ingredients.EnumerateArray().Any(entry =>
                        entry.TryGetProperty("IngredientId", out var id) && id.TryGetGuid(out var snapshotIngredientId) &&
                        snapshotIngredientId == ingredientId);
                if (recipeIngredientExists) return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
        else if (item.RecipeId.HasValue)
        {
            var legacyRecipeIngredientExists = await _dbContext.RecipeItems.AnyAsync(
                recipeItem => recipeItem.RecipeId == item.RecipeId.Value && recipeItem.IngredientId == ingredientId,
                cancellationToken);
            if (legacyRecipeIngredientExists) return true;
        }

        return await (
            from optionRequirement in _dbContext.OrderItemOptionIngredientRequirements
            join option in _dbContext.OrderItemOptions on optionRequirement.OrderItemOptionId equals option.Id
            where option.OrderItemId == orderItemId && optionRequirement.IngredientId == ingredientId
            select optionRequirement.Id).AnyAsync(cancellationToken);
    }

    public Task<StockMovement?> GetStockMovementBySourceEventIdAsync(
        Guid sourceEventId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.StockMovements.AsNoTracking().FirstOrDefaultAsync(
            movement => movement.SourceEventId == sourceEventId,
            cancellationToken);
    }

    public async Task<List<ProductionExecutionRecord>> ListProductionExecutionRecordsAsync(
        Guid sourceCommandId,
        Guid orderItemId,
        CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.ProductionExecutionRecords
            .Where(record =>
                record.SourceCommandId == sourceCommandId &&
                record.OrderItemId == orderItemId)
            .OrderBy(record => record.ProductionUnitNo)
            .ThenBy(record => record.SourceProductionJobId)
            .ToListAsync(cancellationToken);

        // A new job record is aggregated before SaveChanges so lifecycle and
        // physical evidence commit in the same report-ingestion transaction.
        foreach (var local in _dbContext.ChangeTracker.Entries<ProductionExecutionRecord>()
                     .Where(entry => entry.State == EntityState.Added)
                     .Select(entry => entry.Entity)
                     .Where(record =>
                         record.SourceCommandId == sourceCommandId &&
                         record.OrderItemId == orderItemId &&
                         records.All(candidate => candidate.Id != record.Id)))
        {
            records.Add(local);
        }

        return records
            .OrderBy(record => record.ProductionUnitNo)
            .ThenBy(record => record.SourceProductionJobId)
            .ToList();
    }

    public async Task<List<ProductionExecutionRecord>> ListProductionExecutionRecordsForOrderItemAsync(
        Guid orderId,
        Guid orderItemId,
        CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.ProductionExecutionRecords
            .Include(record => record.SourceCommand)
            .Where(record =>
                record.SourceCommand.OrderId == orderId &&
                record.OrderItemId == orderItemId)
            .OrderBy(record => record.SourceCommand.DispatchAttemptNo)
            .ThenBy(record => record.ProductionUnitNo)
            .ToListAsync(cancellationToken);

        foreach (var local in _dbContext.ChangeTracker.Entries<ProductionExecutionRecord>()
                     .Where(entry => entry.State == EntityState.Added)
                     .Select(entry => entry.Entity)
                     .Where(record =>
                         record.OrderItemId == orderItemId &&
                         record.SourceCommandId != Guid.Empty &&
                         records.All(candidate => candidate.Id != record.Id)))
        {
            records.Add(local);
        }

        return records;
    }

    public Task AddStockMovementAsync(
        StockMovement movement,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.StockMovements.AddAsync(movement, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
