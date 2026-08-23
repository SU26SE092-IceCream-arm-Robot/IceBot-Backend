using Application.Catalog.Abstractions;
using Application.Catalog.Images;
using Domain.Catalog.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Catalog.Images;

public sealed class CatalogImageCleanupProcessor
{
    private readonly IProductStore _store;
    private readonly ICatalogImageStorage _storage;
    private readonly ILogger<CatalogImageCleanupProcessor> _logger;

    public CatalogImageCleanupProcessor(
        IProductStore store,
        ICatalogImageStorage storage,
        ILogger<CatalogImageCleanupProcessor> logger)
    {
        _store = store;
        _storage = storage;
        _logger = logger;
    }

    public async Task<CatalogImageCleanupRunResult> ProcessAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var candidates = await _store.ListPendingCatalogImageCleanupsAsync(
            Math.Clamp(batchSize, 1, 500), now, cancellationToken);
        var completedCount = 0;
        var failedCount = 0;

        foreach (var cleanup in candidates)
        {
            if (cleanup.CatalogImageAsset.Status == CatalogImageAssetStatus.Deleted ||
                await _store.IsCatalogImageAssetReferencedAsync(cleanup.CatalogImageAssetId, cancellationToken))
            {
                cleanup.CompletedAt = now;
                cleanup.LastErrorCode = null;
                await _store.SaveChangesAsync(cancellationToken);
                completedCount++;
                continue;
            }

            try
            {
                await _storage.DeleteAsync(cleanup.PublicIdSnapshot, cancellationToken);
                cleanup.CatalogImageAsset.Status = CatalogImageAssetStatus.Deleted;
                cleanup.CatalogImageAsset.UpdatedAt = now;
                cleanup.CompletedAt = now;
                cleanup.LastErrorCode = null;
                await _store.SaveChangesAsync(cancellationToken);
                completedCount++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                cleanup.AttemptCount++;
                cleanup.NextAttemptAt = now.Add(Backoff(cleanup.AttemptCount));
                cleanup.LastErrorCode = "CATALOG_IMAGE_DELETE_FAILED";
                await _store.SaveChangesAsync(cancellationToken);
                failedCount++;
                _logger.LogWarning(exception,
                    "Catalog image cleanup failed for asset {CatalogImageAssetId}; retry {AttemptCount} is scheduled at {NextAttemptAt}.",
                    cleanup.CatalogImageAssetId,
                    cleanup.AttemptCount,
                    cleanup.NextAttemptAt);
            }
        }

        return new CatalogImageCleanupRunResult(candidates.Count, completedCount, failedCount);
    }

    private static TimeSpan Backoff(int attemptCount) =>
        TimeSpan.FromMinutes(Math.Min(24 * 60, Math.Pow(2, Math.Min(attemptCount, 10))));
}

public sealed record CatalogImageCleanupRunResult(int CandidateCount, int CompletedCount, int FailedCount);
