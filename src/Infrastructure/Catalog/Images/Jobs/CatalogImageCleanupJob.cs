using Infrastructure.Concurrency;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Catalog.Images.Jobs;

public sealed class CatalogImageCleanupJob : BackgroundService
{
    private const long CleanupLockKey = 0x494345424F544341L;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CatalogImageCleanupOptions _options;
    private readonly ILogger<CatalogImageCleanupJob> _logger;

    public CatalogImageCleanupJob(
        IServiceScopeFactory scopeFactory,
        IOptions<CatalogImageCleanupOptions> options,
        ILogger<CatalogImageCleanupJob> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Catalog image cleanup is disabled.");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Clamp(_options.IntervalMinutes, 1, 24 * 60));
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
            catch (Exception exception)
            {
                _logger.LogError(exception, "Catalog image cleanup run failed.");
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
        var lockManager = scope.ServiceProvider.GetRequiredService<PostgresAdvisoryLockManager>();
        await using var cleanupLock = await lockManager.TryAcquireAsync(CleanupLockKey, cancellationToken);
        if (cleanupLock is null)
        {
            _logger.LogDebug("Catalog image cleanup skipped because another backend instance holds the lock.");
            return;
        }

        var processor = scope.ServiceProvider.GetRequiredService<CatalogImageCleanupProcessor>();
        var result = await processor.ProcessAsync(_options.BatchSize, cancellationToken);
        _logger.LogInformation(
            "Catalog image cleanup processed {CandidateCount} candidates: {CompletedCount} completed, {FailedCount} failed.",
            result.CandidateCount,
            result.CompletedCount,
            result.FailedCount);
    }
}
