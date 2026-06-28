using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Jobs;

public sealed class DataRetentionJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataRetentionJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    public DataRetentionJob(IServiceScopeFactory scopeFactory, ILogger<DataRetentionJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Data retention background job started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunRetentionPurgeAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during data retention purge.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Data retention background job stopped.");
    }

    private async Task RunRetentionPurgeAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IceBotDbContext>();

        var now = DateTimeOffset.UtcNow;
        var heartbeatThreshold = now.AddDays(-30);
        var deviceEventThreshold = now.AddDays(-90);

        _logger.LogInformation("Purging raw heartbeats older than {HeartbeatThreshold} and device events older than {DeviceEventThreshold}...", heartbeatThreshold, deviceEventThreshold);

        // Batch delete raw heartbeats older than 30 days by heartbeat reporting time.
        var deletedHeartbeats = await dbContext.KioskHeartbeats
            .Where(x => x.ReportedAt < heartbeatThreshold)
            .ExecuteDeleteAsync(cancellationToken);

        // Batch delete raw device events older than 90 days by event occurrence time.
        var deletedDeviceEvents = await dbContext.DeviceEvents
            .Where(x => x.OccurredAt < deviceEventThreshold)
            .ExecuteDeleteAsync(cancellationToken);

        var deletedExecutionRequestNonces = await dbContext.ExecutionEndpointRequestNonces
            .Where(x => x.ExpiresAt < now)
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation(
            "Purge completed. Deleted {DeletedHeartbeats} heartbeats, {DeletedDeviceEvents} device events, and {DeletedExecutionRequestNonces} expired execution request nonces.",
            deletedHeartbeats,
            deletedDeviceEvents,
            deletedExecutionRequestNonces);
    }
}
