using Domain.Sync.Ingestion;
using Domain.Devices.ExecutionEndpoints;
using Application.EdgeIntegration.Abstractions;
using Domain.Devices.Entities;
using Domain.Inventory.Entities;
using Domain.Orders.Entities;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionExecution.Projections;
using Domain.Sync.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"execution-command:{commandId:D}"}, 0));",
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
        return _dbContext.KioskExecutionEndpoints
            .Include(endpoint => endpoint.CredentialBinding)
            .FirstOrDefaultAsync(endpoint => endpoint.Id == endpointId, cancellationToken);
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
        return _dbContext.Orders.FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);
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

    public Task<bool> StockMovementExistsAsync(
        Guid sourceEventId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.StockMovements.AnyAsync(
            movement => movement.SourceEventId == sourceEventId,
            cancellationToken);
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
