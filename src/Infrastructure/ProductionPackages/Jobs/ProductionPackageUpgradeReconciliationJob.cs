using Application.ProductionPackages.Upgrades;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.ProductionPackages.Jobs;

public sealed class ProductionPackageUpgradeReconciliationOptions
{
    public const string SectionName = "ProductionPackageUpgrade:Reconciliation";
    public bool Enabled { get; init; } = true;
    public int IntervalSeconds { get; init; } = 60;
    public int MaterializingTimeoutMinutes { get; init; } = 15;
    public int BatchSize { get; init; } = 100;
}

public sealed class ProductionPackageUpgradeReconciliationJob(
    IServiceScopeFactory scopeFactory,
    IOptions<ProductionPackageUpgradeReconciliationOptions> options,
    ILogger<ProductionPackageUpgradeReconciliationJob> logger) : BackgroundService
{
    private readonly ProductionPackageUpgradeReconciliationOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Production package upgrade reconciliation is disabled.");
            return;
        }

        var interval = TimeSpan.FromSeconds(_options.IntervalSeconds);
        var timeout = TimeSpan.FromMinutes(_options.MaterializingTimeoutMinutes);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider
                    .GetRequiredService<ProductionPackageUpgradeReconciliationService>();
                var result = await service.ReconcileAsync(
                    DateTimeOffset.UtcNow, timeout, _options.BatchSize, stoppingToken);
                if (result.FailedCount > 0)
                    logger.LogWarning(
                        "Marked {FailedCount} of {CandidateCount} stale production package upgrades Failed.",
                        result.FailedCount, result.CandidateCount);
                if (result.ErrorCount > 0)
                    logger.LogError(
                        "Failed to reconcile {ErrorCount} of {CandidateCount} stale production package upgrades; later candidates were still processed.",
                        result.ErrorCount, result.CandidateCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Production package upgrade reconciliation failed.");
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
}
