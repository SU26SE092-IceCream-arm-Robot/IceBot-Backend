using Domain.Devices.Telemetry;
using Application.EdgeIntegration.Abstractions;
using Domain.Devices.Catalog;
using Domain.Orders.Entities;
using Domain.ProductionExecution.Enums;
using Domain.ProductionExecution.Projections;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Application.Orders.Support;
using Application.EdgeIntegration.CommandDelivery.Services;

namespace Infrastructure.EdgeIntegration.Persistence;

public sealed class OrderExecutionTimeoutStore : IOrderExecutionTimeoutStore
{
    private readonly IceBotDbContext _dbContext;

    public OrderExecutionTimeoutStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> ExecuteSerializedAsync<T>(
        Guid sourceCommandId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({EdgeCommandConcurrency.CommandLockKey(sourceCommandId)}, 0));",
            cancellationToken);
        var result = await action(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<Guid>> ListCandidateCommandIdsAsync(
        DateTimeOffset observedAt,
        DateTimeOffset acceptedCutoff,
        DateTimeOffset runningCutoff,
        int maxCommands,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EdgeCommands.AsNoTracking()
            .Where(command =>
                command.CommandType == EdgeCommandType.ExecuteOrder &&
                ((command.CommandExpiryAt < observedAt &&
                    (command.Status == EdgeCommandStatus.PendingDelivery || command.Status == EdgeCommandStatus.Delivered)) ||
                 (command.Status == EdgeCommandStatus.Accepted &&
                    ((!_dbContext.OrderExecutionRecords.Any(record => record.SourceCommandId == command.Id) &&
                        command.RespondedAt <= acceptedCutoff) ||
                     _dbContext.OrderExecutionRecords.Any(record =>
                         record.SourceCommandId == command.Id &&
                         ((record.Status == ProductionExecutionStatus.Accepted && record.LastExecutorReportedAt <= acceptedCutoff) ||
                          (record.Status == ProductionExecutionStatus.Running && record.LastExecutorReportedAt <= runningCutoff)))))))
            .OrderBy(command => command.CommandExpiryAt)
            .ThenBy(command => command.RespondedAt)
            .ThenBy(command => command.Id)
            .Select(command => command.Id)
            .Take(maxCommands)
            .ToListAsync(cancellationToken);
    }

    public Task<EdgeCommand?> GetCommandAsync(
        Guid sourceCommandId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.EdgeCommands
            .Include(command => command.TargetExecutionEndpoint)
            .FirstOrDefaultAsync(command => command.Id == sourceCommandId, cancellationToken);
    }

    public Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders.FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);
    }

    public Task AcquireOrderWorkflowLockAsync(
        Guid orderId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({OrderWorkflowConcurrency.OrderLockKey(orderId)}, 0));",
            cancellationToken);

    public Task<OrderExecutionRecord?> GetOrderExecutionRecordAsync(
        Guid sourceCommandId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.OrderExecutionRecords.FirstOrDefaultAsync(
            record => record.SourceCommandId == sourceCommandId,
            cancellationToken);
    }

    public Task<KioskHeartbeat?> GetLatestHeartbeatAsync(
        Guid kioskId,
        Guid sourceExecutorId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskHeartbeats.AsNoTracking()
            .Where(heartbeat => heartbeat.KioskId == kioskId && heartbeat.NodeId == sourceExecutorId)
            .OrderByDescending(heartbeat => heartbeat.ReceivedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task AddOrderExecutionRecordAsync(
        OrderExecutionRecord record,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.OrderExecutionRecords.AddAsync(record, cancellationToken).AsTask();
    }

    public Task AddOrderStatusHistoryAsync(
        OrderStatusHistory history,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.OrderStatusHistories.AddAsync(history, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
