using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.RuntimeMenus.Abstractions;
using Infrastructure.SalesCatalog.Caching;
using Infrastructure.SalesCatalog.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.SalesCatalog;

public static class SalesCatalogInfrastructureModule
{
    public static IServiceCollection AddSalesCatalogInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IMenuStore, MenuStore>();
        services.AddOptions<RuntimeMenuCacheOptions>()
            .Bind(config.GetSection(RuntimeMenuCacheOptions.SectionName))
            .Validate(options =>
                    options.DistributedExpirationSeconds >= 1 &&
                    options.DistributedExpirationSeconds <= 60 &&
                    options.LocalExpirationSeconds >= 1 &&
                    options.LocalExpirationSeconds <= options.DistributedExpirationSeconds &&
                    options.UncachedSnapshotExpirationSeconds >= 1 &&
                    options.UncachedSnapshotExpirationSeconds <= 60,
                "Runtime-menu cache expiration settings are invalid.")
            .Validate(options =>
                    !options.Enabled ||
                    (!string.IsNullOrWhiteSpace(options.RedisConnectionString) &&
                     !string.IsNullOrWhiteSpace(options.InstanceName)),
                "Runtime-menu Redis connection string and instance name are required when the cache is enabled.")
            .ValidateOnStart();

        var cacheOptions = config.GetSection(RuntimeMenuCacheOptions.SectionName).Get<RuntimeMenuCacheOptions>()
            ?? new RuntimeMenuCacheOptions();
        if (cacheOptions.Enabled)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cacheOptions.RedisConnectionString;
                options.InstanceName = cacheOptions.InstanceName;
            });
            services.AddHybridCache();
        }

        services.AddSingleton<IRuntimeMenuProjectionCache, RuntimeMenuProjectionCache>();
        return services;
    }
}
