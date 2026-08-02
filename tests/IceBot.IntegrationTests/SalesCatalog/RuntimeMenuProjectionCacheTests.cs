using Application.SalesCatalog.RuntimeMenus.Results;
using Infrastructure.SalesCatalog.Caching;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IceBot.IntegrationTests.SalesCatalog;

public sealed class RuntimeMenuProjectionCacheTests
{
    [Fact]
    public async Task EnabledCache_ReusesProjectionWithinItsExpiry()
    {
        using var services = CreateServices();
        var cache = CreateCache(services);
        var builds = 0;

        var first = await cache.GetOrCreateAsync(firstKeyKioskId, _ =>
        {
            builds++;
            return Task.FromResult(new RuntimeMenuProjection("revision-1", []));
        });
        var second = await cache.GetOrCreateAsync(firstKeyKioskId, _ =>
        {
            builds++;
            return Task.FromResult(new RuntimeMenuProjection("revision-2", []));
        });

        Assert.Equal(1, builds);
        Assert.Equal("revision-1", first.Revision);
        Assert.Equal("revision-1", second.Revision);
    }

    [Fact]
    public async Task SourceProjectionFailure_IsNotRetriedAsCacheFallback()
    {
        using var services = CreateServices();
        var cache = CreateCache(services);
        var attempts = 0;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cache.GetOrCreateAsync(Guid.NewGuid(), _ =>
            {
                attempts++;
                throw new InvalidOperationException("database projection failed");
            }));

        Assert.Equal("database projection failed", exception.Message);
        Assert.Equal(1, attempts);
    }

    private static readonly Guid firstKeyKioskId = Guid.Parse("a1a5fb5e-6a5a-4aac-865f-c7f051416066");

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddHybridCache();
        return services.BuildServiceProvider();
    }

    private static RuntimeMenuProjectionCache CreateCache(ServiceProvider services)
    {
        return new RuntimeMenuProjectionCache(
            Options.Create(new RuntimeMenuCacheOptions
            {
                Enabled = true,
                DistributedExpirationSeconds = 10,
                LocalExpirationSeconds = 1,
                UncachedSnapshotExpirationSeconds = 15
            }),
            services.GetRequiredService<ILogger<RuntimeMenuProjectionCache>>(),
            services.GetRequiredService<HybridCache>());
    }
}
