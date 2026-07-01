using Domain.Sync.Ingestion;
using Domain.Devices.Telemetry;
using Domain.Sync.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Persistence.Jobs;

public sealed record DataRetentionPurgeResult(
    int Heartbeats,
    int DeviceEvents,
    int OperationLogs,
    int SyncInboxReceipts,
    int ExecutionRequestNonces);

public sealed class DataRetentionPurger
{
    private readonly IceBotDbContext _dbContext;
    private readonly DataRetentionOptions _options;

    public DataRetentionPurger(IceBotDbContext dbContext, IOptions<DataRetentionOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;
    }

    public async Task<DataRetentionPurgeResult> PurgeAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var heartbeats = await PurgeHeartbeatsAsync(now.AddDays(-_options.HeartbeatDays), cancellationToken);
        var deviceEvents = await PurgeDeviceEventsAsync(now.AddDays(-_options.DeviceEventDays), cancellationToken);
        var operationLogs = await PurgeOperationLogsAsync(now.AddDays(-_options.OperationLogDays), cancellationToken);
        var syncInbox = await PurgeSyncInboxAsync(now.AddDays(-_options.ProcessedSyncInboxDays), cancellationToken);
        var nonces = await PurgeRequestNoncesAsync(now, cancellationToken);
        return new DataRetentionPurgeResult(heartbeats, deviceEvents, operationLogs, syncInbox, nonces);
    }

    private async Task<int> PurgeHeartbeatsAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        var total = 0;
        for (var batch = 0; batch < _options.MaxBatchesPerRun; batch++)
        {
            var ids = await _dbContext.KioskHeartbeats.AsNoTracking()
                .Where(item => item.ReportedAt < cutoff)
                .OrderBy(item => item.ReportedAt)
                .Select(item => item.Id)
                .Take(_options.BatchSize)
                .ToListAsync(cancellationToken);
            if (ids.Count == 0) break;

            var deleted = await _dbContext.KioskHeartbeats
                .Where(item => ids.Contains(item.Id) && item.ReportedAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);
            total += deleted;
            if (deleted == 0 || ids.Count < _options.BatchSize) break;
        }

        return total;
    }

    private async Task<int> PurgeDeviceEventsAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        var total = 0;
        for (var batch = 0; batch < _options.MaxBatchesPerRun; batch++)
        {
            var ids = await _dbContext.DeviceEvents.AsNoTracking()
                .Where(deviceEvent =>
                    deviceEvent.OccurredAt < cutoff &&
                    !_dbContext.MaintenanceTickets.Any(ticket => ticket.DeviceEventId == deviceEvent.Id))
                .OrderBy(deviceEvent => deviceEvent.OccurredAt)
                .Select(deviceEvent => deviceEvent.Id)
                .Take(_options.BatchSize)
                .ToListAsync(cancellationToken);
            if (ids.Count == 0) break;

            var deleted = await _dbContext.DeviceEvents
                .Where(deviceEvent =>
                    ids.Contains(deviceEvent.Id) &&
                    deviceEvent.OccurredAt < cutoff &&
                    !_dbContext.MaintenanceTickets.Any(ticket => ticket.DeviceEventId == deviceEvent.Id))
                .ExecuteDeleteAsync(cancellationToken);
            total += deleted;
            if (deleted == 0 || ids.Count < _options.BatchSize) break;
        }

        return total;
    }

    private async Task<int> PurgeOperationLogsAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        var total = 0;
        for (var batch = 0; batch < _options.MaxBatchesPerRun; batch++)
        {
            var ids = await _dbContext.OperationLogs.AsNoTracking()
                .Where(item => item.OccurredAt < cutoff)
                .OrderBy(item => item.OccurredAt)
                .Select(item => item.Id)
                .Take(_options.BatchSize)
                .ToListAsync(cancellationToken);
            if (ids.Count == 0) break;

            var deleted = await _dbContext.OperationLogs
                .Where(item => ids.Contains(item.Id) && item.OccurredAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);
            total += deleted;
            if (deleted == 0 || ids.Count < _options.BatchSize) break;
        }

        return total;
    }

    private async Task<int> PurgeSyncInboxAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        var total = 0;
        for (var batch = 0; batch < _options.MaxBatchesPerRun; batch++)
        {
            var ids = await _dbContext.SyncEventInbox.AsNoTracking()
                .Where(item =>
                    (item.Status == SyncEventStatus.Processed || item.Status == SyncEventStatus.Ignored) &&
                    (item.ProcessedAt ?? item.ReceivedAt) < cutoff &&
                    !_dbContext.SyncDeadLetters.Any(deadLetter => deadLetter.SyncEventInboxId == item.Id))
                .OrderBy(item => item.ProcessedAt ?? item.ReceivedAt)
                .Select(item => item.Id)
                .Take(_options.BatchSize)
                .ToListAsync(cancellationToken);
            if (ids.Count == 0) break;

            var deleted = await _dbContext.SyncEventInbox
                .Where(item =>
                    ids.Contains(item.Id) &&
                    (item.Status == SyncEventStatus.Processed || item.Status == SyncEventStatus.Ignored) &&
                    (item.ProcessedAt ?? item.ReceivedAt) < cutoff &&
                    !_dbContext.SyncDeadLetters.Any(deadLetter => deadLetter.SyncEventInboxId == item.Id))
                .ExecuteDeleteAsync(cancellationToken);
            total += deleted;
            if (deleted == 0 || ids.Count < _options.BatchSize) break;
        }

        return total;
    }

    private async Task<int> PurgeRequestNoncesAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        var total = 0;
        for (var batch = 0; batch < _options.MaxBatchesPerRun; batch++)
        {
            var ids = await _dbContext.ExecutionEndpointRequestNonces.AsNoTracking()
                .Where(item => item.ExpiresAt < cutoff)
                .OrderBy(item => item.ExpiresAt)
                .Select(item => item.Id)
                .Take(_options.BatchSize)
                .ToListAsync(cancellationToken);
            if (ids.Count == 0) break;

            var deleted = await _dbContext.ExecutionEndpointRequestNonces
                .Where(item => ids.Contains(item.Id) && item.ExpiresAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);
            total += deleted;
            if (deleted == 0 || ids.Count < _options.BatchSize) break;
        }

        return total;
    }
}
