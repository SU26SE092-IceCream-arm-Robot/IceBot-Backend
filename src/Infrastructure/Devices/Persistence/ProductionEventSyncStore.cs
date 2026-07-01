using Domain.Sync.Ingestion;
using Application.Devices.Abstractions;
using Domain.Sync.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Devices.Persistence;

public sealed class ProductionEventSyncStore : IProductionEventSyncStore
{
    private readonly IceBotDbContext _dbContext;

    public ProductionEventSyncStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<T> ExecuteHistoryIngestionAsync<T>(
        Guid sourceExecutorId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default) =>
        ExecuteSerializedAsync($"production-history:{sourceExecutorId:D}", action, cancellationToken);

    public Task<T> ExecuteSummaryIngestionAsync<T>(
        Guid sourceExecutorId,
        string summaryKind,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default) =>
        ExecuteSerializedAsync($"edge-state-summary:{sourceExecutorId:D}:{summaryKind}", action, cancellationToken);

    public Task<SyncEventInbox?> GetEventByIdAsync(Guid sourceExecutorId, Guid eventId, CancellationToken cancellationToken = default) =>
        _dbContext.SyncEventInbox.AsNoTracking().FirstOrDefaultAsync(
            item => item.SourceNodeId == sourceExecutorId && item.EventId == eventId,
            cancellationToken);

    public Task<SyncEventInbox?> GetEventBySequenceAsync(
        Guid sourceExecutorId,
        long sequenceNumber,
        CancellationToken cancellationToken = default) =>
        _dbContext.SyncEventInbox.AsNoTracking().FirstOrDefaultAsync(
            item => item.SourceNodeId == sourceExecutorId && item.SequenceNumber == sequenceNumber,
            cancellationToken);

    public Task<ProductionEventCheckpoint?> GetCheckpointAsync(
        Guid sourceExecutorId,
        bool tracked,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ProductionEventCheckpoints.AsQueryable();
        if (!tracked)
        {
            query = query.AsNoTracking();
        }
        return query.FirstOrDefaultAsync(item => item.SourceExecutorId == sourceExecutorId, cancellationToken);
    }

    public Task<List<SyncEventInbox>> ListContiguousCandidatesAsync(
        Guid sourceExecutorId,
        long afterSequenceNumber,
        CancellationToken cancellationToken = default) =>
        _dbContext.SyncEventInbox.AsNoTracking()
            .Where(item => item.SourceNodeId == sourceExecutorId && item.SequenceNumber > afterSequenceNumber)
            .OrderBy(item => item.SequenceNumber)
            .Take(1000)
            .ToListAsync(cancellationToken);

    public Task<EdgeStateSummary?> GetStateSummaryAsync(
        Guid sourceExecutorId,
        string summaryKind,
        bool tracked,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.EdgeStateSummaries.AsQueryable();
        if (!tracked)
        {
            query = query.AsNoTracking();
        }
        return query.FirstOrDefaultAsync(
            item => item.SourceExecutorId == sourceExecutorId && item.SummaryKind == summaryKind,
            cancellationToken);
    }

    public Task AddEventAsync(SyncEventInbox item, CancellationToken cancellationToken = default) =>
        _dbContext.SyncEventInbox.AddAsync(item, cancellationToken).AsTask();

    public Task AddCheckpointAsync(ProductionEventCheckpoint checkpoint, CancellationToken cancellationToken = default) =>
        _dbContext.ProductionEventCheckpoints.AddAsync(checkpoint, cancellationToken).AsTask();

    public Task AddStateSummaryAsync(EdgeStateSummary summary, CancellationToken cancellationToken = default) =>
        _dbContext.EdgeStateSummaries.AddAsync(summary, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    private async Task<T> ExecuteSerializedAsync<T>(
        string lockKey,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0));",
            cancellationToken);
        var result = await action(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
