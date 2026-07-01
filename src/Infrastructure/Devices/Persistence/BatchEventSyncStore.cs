using Domain.Sync.Ingestion;
using Application.Devices.Abstractions;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Devices.Persistence;

public sealed class BatchEventSyncStore : IBatchEventSyncStore
{
    private readonly IceBotDbContext _dbContext;

    public BatchEventSyncStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SyncEventInbox?> GetReceiptAsync(Guid sourceNodeId, Guid eventId, CancellationToken cancellationToken = default) =>
        _dbContext.SyncEventInbox.AsNoTracking()
            .FirstOrDefaultAsync(item => item.SourceNodeId == sourceNodeId && item.EventId == eventId, cancellationToken);

    public async Task<bool> RecordProcessedReceiptAsync(
        Guid eventId,
        Guid kioskId,
        Guid sourceNodeId,
        string eventType,
        DateTimeOffset occurredAt,
        string aggregateType,
        Guid aggregateId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var lockKey = $"batch-sync-receipt:{sourceNodeId:D}:{eventId:D}";
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0));",
            cancellationToken);

        if (await _dbContext.SyncEventInbox.AnyAsync(
                item => item.SourceNodeId == sourceNodeId && item.EventId == eventId,
                cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        var now = DateTimeOffset.UtcNow;
        await _dbContext.SyncEventInbox.AddAsync(new SyncEventInbox
        {
            EventId = eventId,
            KioskId = kioskId,
            SourceNodeId = sourceNodeId,
            EventType = eventType,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            PayloadJson = "{\"transport\":\"BatchEventsV1\"}",
            Status = SyncEventStatus.Processed,
            OccurredAt = occurredAt,
            ReceivedAt = now,
            ProcessedAt = now
        }, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return false;
    }
}
