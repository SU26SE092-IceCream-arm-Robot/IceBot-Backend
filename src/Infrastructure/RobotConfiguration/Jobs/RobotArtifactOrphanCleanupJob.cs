using Application.RobotConfiguration.Abstractions;
using Infrastructure.RobotConfiguration.ObjectStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.RobotConfiguration.Jobs;

public sealed class RobotArtifactOrphanCleanupJob : BackgroundService
{
    private const string ArtifactPrefix = "robot-artifact";
    private const long CleanupLockKey = 0x494345424F544F52L;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RobotArtifactObjectStorageOptions _options;
    private readonly ILogger<RobotArtifactOrphanCleanupJob> _logger;

    public RobotArtifactOrphanCleanupJob(
        IServiceScopeFactory scopeFactory,
        IOptions<RobotArtifactObjectStorageOptions> options,
        ILogger<RobotArtifactOrphanCleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.OrphanCleanupEnabled)
        {
            _logger.LogInformation("Robot artifact orphan cleanup is disabled.");
            return;
        }

        var interval = TimeSpan.FromHours(_options.OrphanCleanupIntervalHours);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Robot artifact orphan cleanup failed.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var lockManager = scope.ServiceProvider.GetRequiredService<Infrastructure.Concurrency.PostgresAdvisoryLockManager>();
        await using var cleanupLock = await lockManager.TryAcquireAsync(CleanupLockKey, cancellationToken);
        if (cleanupLock is null)
        {
            _logger.LogInformation("Robot artifact orphan cleanup skipped because another backend instance holds the distributed lock.");
            return;
        }

        var storage = scope.ServiceProvider.GetRequiredService<IArtifactObjectStorage>();
        var store = scope.ServiceProvider.GetRequiredService<IRobotConfigurationStore>();
        var templateStore = scope.ServiceProvider.GetRequiredService<IRobotArtifactTemplateStore>();
        var referencedKeys = (await store.ListArtifactStorageKeysAsync(cancellationToken))
            .Concat(await templateStore.ListStorageKeysAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);
        var threshold = DateTimeOffset.UtcNow.AddHours(-_options.OrphanGracePeriodHours);
        var deletedCount = 0;

        await foreach (var item in storage.ListAsync(ArtifactPrefix, cancellationToken))
        {
            if (item.LastModifiedAt >= threshold || referencedKeys.Contains(item.StorageKey))
            {
                continue;
            }

            try
            {
                await storage.DeleteIfExistsAsync(item.StorageKey, cancellationToken);
                deletedCount++;
                _logger.LogInformation(
                    "Deleted orphan robot artifact object {StorageKey} last modified at {LastModifiedAt}.",
                    item.StorageKey,
                    item.LastModifiedAt);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete orphan robot artifact object {StorageKey}.", item.StorageKey);
            }

            if (deletedCount >= _options.OrphanCleanupMaxDeletesPerRun)
            {
                _logger.LogWarning(
                    "Robot artifact orphan cleanup reached the per-run delete limit {DeleteLimit}.",
                    _options.OrphanCleanupMaxDeletesPerRun);
                break;
            }
        }

        _logger.LogInformation(
            "Robot artifact orphan cleanup completed. Deleted {DeletedCount} objects older than {Threshold}.",
            deletedCount,
            threshold);
    }
}
