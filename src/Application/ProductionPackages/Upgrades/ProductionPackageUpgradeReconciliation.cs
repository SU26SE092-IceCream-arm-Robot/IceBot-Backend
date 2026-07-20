namespace Application.ProductionPackages.Upgrades;

public sealed record ProductionPackageUpgradeReconciliationResult(
    int CandidateCount,
    int FailedCount);

public sealed class ProductionPackageUpgradeReconciliationService(
    IProductionPackageUpgradeStore upgrades)
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
        foreach (var id in ids)
        {
            if (await upgrades.TryFailStaleMaterializingAsync(id, cutoff, now, cancellationToken))
                failed++;
        }
        ProductionPackageUpgradeMetrics.RecordReconciliation(failed);
        return new ProductionPackageUpgradeReconciliationResult(ids.Count, failed);
    }
}
