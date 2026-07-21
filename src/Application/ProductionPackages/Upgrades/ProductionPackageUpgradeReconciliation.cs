using Microsoft.Extensions.Logging;

namespace Application.ProductionPackages.Upgrades;

public sealed record ProductionPackageUpgradeReconciliationResult(
    int CandidateCount,
    int FailedCount,
    int ErrorCount);

public sealed class ProductionPackageUpgradeReconciliationService(
    IProductionPackageUpgradeStore upgrades,
    ILogger<ProductionPackageUpgradeReconciliationService>? logger = null)
{
    public async Task<ProductionPackageUpgradeReconciliationResult> ReconcileAsync(
        DateTimeOffset now, TimeSpan progressTimeout, int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (progressTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(progressTimeout));
        batchSize = Math.Clamp(batchSize, 1, 500);
        var cutoff = now - progressTimeout;
        var ids = await upgrades.ListStaleMaterializingIdsAsync(cutoff, batchSize, cancellationToken);
        var failed = 0;
        var errors = 0;
        foreach (var id in ids)
        {
            try
            {
                if (await upgrades.TryFailStaleMaterializingAsync(id, cutoff, now, cancellationToken))
                    failed++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                errors++;
                logger?.LogError(ex,
                    "Failed to reconcile stale production package upgrade {ProductionPackageUpgradeId}.", id);
            }
        }
        ProductionPackageUpgradeMetrics.RecordReconciliation(failed, errors);
        return new ProductionPackageUpgradeReconciliationResult(ids.Count, failed, errors);
    }
}
