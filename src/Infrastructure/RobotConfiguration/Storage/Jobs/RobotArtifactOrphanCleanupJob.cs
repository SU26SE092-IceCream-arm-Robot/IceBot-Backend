using Application.RobotConfiguration.Storage.Services;
using Application.RobotConfiguration.Storage.Abstractions;
using Application.RobotConfiguration.Artifacts.Abstractions;
using Infrastructure.RobotConfiguration.Storage.ObjectStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Infrastructure.Operations.Automation;
using System.Diagnostics;

namespace Infrastructure.RobotConfiguration.Storage.Jobs;

public sealed class RobotArtifactOrphanCleanupJob : BackgroundService
{
    private static readonly string[] ManagedPrefixes = ["robot-artifact", "robot-authoring-imports"];
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
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await CleanupAsync(stoppingToken);
                OperationalAutomationMetrics.RecordRun(
                    "robot_artifact_orphan_cleanup",
                    !result.Completed
                        ? "skipped"
                        : result.CandidateFailureCount == 0 ? "succeeded" : "partial_failure",
                    stopwatch.Elapsed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                OperationalAutomationMetrics.RecordRun(
                    "robot_artifact_orphan_cleanup", "failed", stopwatch.Elapsed);
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

    private async Task<RobotArtifactCleanupResult> CleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var lockManager = scope.ServiceProvider.GetRequiredService<Infrastructure.Concurrency.PostgresAdvisoryLockManager>();
        await using var cleanupLock = await lockManager.TryAcquireAsync(CleanupLockKey, cancellationToken);
        if (cleanupLock is null)
        {
            _logger.LogInformation("Robot artifact orphan cleanup skipped because another backend instance holds the distributed lock.");
            return new RobotArtifactCleanupResult(false, 0);
        }

        var storage = scope.ServiceProvider.GetRequiredService<IArtifactObjectStorage>();
        var referenceSources = scope.ServiceProvider.GetServices<IArtifactObjectReferenceSource>();
        var referencedKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var referenceSource in referenceSources)
        {
            referencedKeys.UnionWith(await referenceSource.ListReferencedStorageKeysAsync(cancellationToken));
        }
        var threshold = DateTimeOffset.UtcNow.AddHours(-_options.OrphanGracePeriodHours);
        var deletedCount = 0;
        var candidateFailures = 0;

        foreach (var prefix in ManagedPrefixes)
        {
            await foreach (var item in storage.ListAsync(prefix, cancellationToken))
            {
                if (item.LastModifiedAt >= threshold || referencedKeys.Contains(item.StorageKey))
                    continue;

                try
                {
                    await storage.DeleteIfExistsAsync(item.StorageKey, cancellationToken);
                    deletedCount++;
                    _logger.LogInformation(
                        "Deleted unreferenced robot configuration object {StorageKey} last modified at {LastModifiedAt}.",
                        item.StorageKey,
                        item.LastModifiedAt);
                }
                catch (Exception ex)
                {
                    candidateFailures++;
                    OperationalAutomationMetrics.RecordCandidateFailure("robot_artifact_orphan_cleanup");
                    _logger.LogWarning(ex, "Failed to delete unreferenced robot configuration object {StorageKey}.", item.StorageKey);
                }

                if (deletedCount >= _options.OrphanCleanupMaxDeletesPerRun)
                {
                    _logger.LogWarning(
                        "Robot configuration object cleanup reached the per-run delete limit {DeleteLimit}.",
                        _options.OrphanCleanupMaxDeletesPerRun);
                    break;
                }
            }

            if (deletedCount >= _options.OrphanCleanupMaxDeletesPerRun) break;
        }

        _logger.LogInformation(
            "Robot artifact orphan cleanup completed. Deleted {DeletedCount} objects older than {Threshold}.",
            deletedCount,
            threshold);
        return new RobotArtifactCleanupResult(true, candidateFailures);
    }

    private sealed record RobotArtifactCleanupResult(bool Completed, int CandidateFailureCount);
}
