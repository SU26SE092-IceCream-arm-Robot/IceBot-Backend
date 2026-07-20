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
    int ExecutionRequestNonces,
    int RefreshTokens,
    int PasswordResetRequests,
    int AccountInvitations,
    int NotificationDeliveries);

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
        var identityCutoff = now.AddDays(-_options.ExpiredIdentityCredentialDays);
        var refreshTokens = await PurgeRefreshTokensAsync(now, identityCutoff, cancellationToken);
        var passwordResets = await PurgePasswordResetRequestsAsync(now, identityCutoff, cancellationToken);
        var invitations = await PurgeAccountInvitationsAsync(now, identityCutoff, cancellationToken);
        var notificationDeliveries = await PurgeNotificationDeliveriesAsync(
            now.AddDays(-_options.NotificationDeliveryDays), cancellationToken);
        return new DataRetentionPurgeResult(
            heartbeats, deviceEvents, operationLogs, syncInbox, nonces,
            refreshTokens, passwordResets, invitations, notificationDeliveries);
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

    private async Task<int> PurgeRefreshTokensAsync(
        DateTimeOffset now,
        DateTimeOffset cutoff,
        CancellationToken cancellationToken)
    {
        var total = 0;
        for (var batch = 0; batch < _options.MaxBatchesPerRun; batch++)
        {
            var ids = await _dbContext.RefreshTokens.AsNoTracking()
                .Where(token =>
                    token.ExpiresAt < now &&
                    token.ExpiresAt < cutoff &&
                    !_dbContext.RefreshTokens.Any(other => other.ReplacedByTokenId == token.Id))
                .OrderBy(token => token.ExpiresAt)
                .Select(token => token.Id)
                .Take(_options.BatchSize)
                .ToListAsync(cancellationToken);
            if (ids.Count == 0) break;

            var deleted = await _dbContext.RefreshTokens
                .Where(token =>
                    ids.Contains(token.Id) &&
                    token.ExpiresAt < now &&
                    token.ExpiresAt < cutoff &&
                    !_dbContext.RefreshTokens.Any(other => other.ReplacedByTokenId == token.Id))
                .ExecuteDeleteAsync(cancellationToken);
            total += deleted;
            if (deleted == 0 || ids.Count < _options.BatchSize) break;
        }

        return total;
    }

    private Task<int> PurgePasswordResetRequestsAsync(
        DateTimeOffset now,
        DateTimeOffset cutoff,
        CancellationToken cancellationToken) =>
        PurgeIdentityRowsAsync(
            () => _dbContext.PasswordResetRequests.AsNoTracking()
                .Where(item => item.ExpiresAt < now && item.ExpiresAt < cutoff)
                .OrderBy(item => item.ExpiresAt)
                .Select(item => item.Id),
            ids => _dbContext.PasswordResetRequests
                .Where(item => ids.Contains(item.Id) && item.ExpiresAt < now && item.ExpiresAt < cutoff),
            cancellationToken);

    private Task<int> PurgeAccountInvitationsAsync(
        DateTimeOffset now,
        DateTimeOffset cutoff,
        CancellationToken cancellationToken) =>
        PurgeIdentityRowsAsync(
            () => _dbContext.AccountInvitations.AsNoTracking()
                .Where(item => item.ExpiresAt < now && item.ExpiresAt < cutoff)
                .OrderBy(item => item.ExpiresAt)
                .Select(item => item.Id),
            ids => _dbContext.AccountInvitations
                .Where(item => ids.Contains(item.Id) && item.ExpiresAt < now && item.ExpiresAt < cutoff),
            cancellationToken);

    private async Task<int> PurgeIdentityRowsAsync<T>(
        Func<IQueryable<Guid>> candidateIds,
        Func<IReadOnlyCollection<Guid>, IQueryable<T>> rows,
        CancellationToken cancellationToken)
        where T : class
    {
        var total = 0;
        for (var batch = 0; batch < _options.MaxBatchesPerRun; batch++)
        {
            var ids = await candidateIds().Take(_options.BatchSize).ToListAsync(cancellationToken);
            if (ids.Count == 0) break;

            var deleted = await rows(ids).ExecuteDeleteAsync(cancellationToken);
            total += deleted;
            if (deleted == 0 || ids.Count < _options.BatchSize) break;
        }

        return total;
    }

    private Task<int> PurgeNotificationDeliveriesAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken) =>
        PurgeIdentityRowsAsync(
            () => _dbContext.NotificationDeliveries.AsNoTracking()
                .Where(item =>
                    !Application.Operations.Notifications.NotificationDeliveryRetentionPolicy
                        .DurableEvidenceNotificationTypes.Contains(item.NotificationType) &&
                    (item.Status == Domain.Operations.Enums.NotificationDeliveryStatus.Delivered ||
                     item.Status == Domain.Operations.Enums.NotificationDeliveryStatus.PermanentFailure) &&
                    (item.UpdatedAt ?? item.CreatedAt) < cutoff)
                .OrderBy(item => item.UpdatedAt ?? item.CreatedAt)
                .Select(item => item.Id),
            ids => _dbContext.NotificationDeliveries.Where(item =>
                ids.Contains(item.Id) &&
                !Application.Operations.Notifications.NotificationDeliveryRetentionPolicy
                    .DurableEvidenceNotificationTypes.Contains(item.NotificationType) &&
                (item.Status == Domain.Operations.Enums.NotificationDeliveryStatus.Delivered ||
                 item.Status == Domain.Operations.Enums.NotificationDeliveryStatus.PermanentFailure) &&
                (item.UpdatedAt ?? item.CreatedAt) < cutoff),
            cancellationToken);
}
