using Application.EdgeIntegration.Abstractions;
using Domain.Devices.Entities;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionExecution.Projections;
using Domain.Sync.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EdgeIntegration.Persistence;

public sealed class ExecutionReportStore : IExecutionReportStore
{
    private readonly IceBotDbContext _dbContext;

    public ExecutionReportStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> ExecuteReportIngestionAsync<T>(
        Guid sourceEventId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"execution-report:{sourceEventId:D}"}, 0));",
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
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SyncEventInbox
            .FirstOrDefaultAsync(syncEvent => syncEvent.EventId == eventId, cancellationToken);
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
        Guid? sourceProductionJobId,
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

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
