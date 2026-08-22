using System.Diagnostics.Metrics;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Application.SalesCatalog.RuntimeMenus.Abstractions;
using Application.SalesCatalog.RuntimeMenus.Results;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.SalesCatalog.Caching;

public sealed class RuntimeMenuProjectionCache : IRuntimeMenuProjectionCache
{
    public const string MeterName = "IceBot.SalesCatalog.RuntimeMenu";

    private static readonly Meter Meter = new(MeterName, "1.0.0");
    private static readonly Counter<long> Requests = Meter.CreateCounter<long>("icebot.runtime_menu.cache.requests");
    private static readonly Counter<long> Failures = Meter.CreateCounter<long>("icebot.runtime_menu.cache.failures");
    private static readonly Histogram<double> BuildDuration = Meter.CreateHistogram<double>(
        "icebot.runtime_menu.cache.build.duration", "ms");

    private readonly HybridCache? _cache;
    private readonly RuntimeMenuCacheOptions _options;
    private readonly ILogger<RuntimeMenuProjectionCache> _logger;

    public RuntimeMenuProjectionCache(
        IOptions<RuntimeMenuCacheOptions> options,
        ILogger<RuntimeMenuProjectionCache> logger,
        HybridCache? cache = null)
    {
        _options = options.Value;
        _logger = logger;
        _cache = cache;
    }

    public async Task<RuntimeMenuCachedProjection> GetOrCreateAsync(
        Guid kioskId,
        Func<CancellationToken, Task<RuntimeMenuProjection>> factory,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || _cache is null)
        {
            Requests.Add(1, new KeyValuePair<string, object?>("cache.outcome", "disabled"));
            return await BuildAsync(factory, _options.UncachedSnapshotExpirationSeconds, cancellationToken);
        }

        var built = false;
        Exception? projectionFailure = null;
        RuntimeMenuCachedProjection? newlyBuiltProjection = null;
        var key = $"runtime-menu:v1:kiosk:{kioskId:N}";
        try
        {
            var result = await _cache.GetOrCreateAsync(
                key,
                async ct =>
                {
                    built = true;
                    try
                    {
                        newlyBuiltProjection = await BuildAsync(factory, _options.DistributedExpirationSeconds, ct);
                        return newlyBuiltProjection;
                    }
                    catch (Exception exception)
                    {
                        projectionFailure = exception;
                        throw;
                    }
                },
                new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromSeconds(_options.DistributedExpirationSeconds),
                    LocalCacheExpiration = TimeSpan.FromSeconds(_options.LocalExpirationSeconds)
                },
                cancellationToken: cancellationToken);
            Requests.Add(1, new KeyValuePair<string, object?>("cache.outcome", built ? "miss" : "hit"));
            return result;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            if (projectionFailure is not null)
            {
                ExceptionDispatchInfo.Capture(projectionFailure).Throw();
            }

            Failures.Add(1);
            _logger.LogWarning(
                exception,
                "Runtime-menu cache failed for kiosk {KioskId}; falling back to the database projection.",
                kioskId);
            Requests.Add(1, new KeyValuePair<string, object?>("cache.outcome", "fallback"));
            if (newlyBuiltProjection is not null)
            {
                return newlyBuiltProjection;
            }

            return await BuildAsync(factory, _options.UncachedSnapshotExpirationSeconds, cancellationToken);
        }
    }

    public async Task InvalidateAsync(Guid kioskId, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || _cache is null)
        {
            return;
        }

        await _cache.RemoveAsync($"runtime-menu:v1:kiosk:{kioskId:N}", cancellationToken);
    }

    private static async Task<RuntimeMenuCachedProjection> BuildAsync(
        Func<CancellationToken, Task<RuntimeMenuProjection>> factory,
        int expirationSeconds,
        CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();
        var projection = await factory(cancellationToken);
        BuildDuration.Record(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
        return new RuntimeMenuCachedProjection(
            projection.Revision,
            projection.Items,
            DateTimeOffset.UtcNow.AddSeconds(expirationSeconds));
    }
}
