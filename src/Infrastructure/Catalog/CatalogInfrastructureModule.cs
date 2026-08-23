using Application.Catalog.Abstractions;
using Application.Catalog.Images;
using Infrastructure.Catalog.Images;
using Infrastructure.Catalog.Images.Jobs;
using Microsoft.Extensions.Configuration;
using Infrastructure.Catalog.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Catalog;

public static class CatalogInfrastructureModule
{
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IProductStore, ProductStore>();
        services.AddOptions<CloudinaryCatalogImageStorageOptions>()
            .Bind(configuration.GetSection(CloudinaryCatalogImageStorageOptions.SectionName));
        services.AddOptions<CatalogImageCleanupOptions>()
            .Bind(configuration.GetSection(CatalogImageCleanupOptions.SectionName));
        services.AddScoped<CloudinaryCatalogImageStorage>();
        services.AddScoped<ICatalogImageStorage>(provider => provider.GetRequiredService<CloudinaryCatalogImageStorage>());
        services.AddScoped<ICatalogImageStorageHealthProbe>(provider => provider.GetRequiredService<CloudinaryCatalogImageStorage>());
        services.AddScoped<ICatalogImageMutationCoordinator, PostgresCatalogImageMutationCoordinator>();
        services.AddScoped<CatalogImageCleanupProcessor>();
        services.AddScoped<ICatalogAuthoringStore, CatalogAuthoringStore>();
        services.AddHostedService<CatalogImageCleanupJob>();
        return services;
    }
}
