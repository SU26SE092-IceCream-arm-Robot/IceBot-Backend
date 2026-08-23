using Application.Catalog.Abstractions;
using Application.Catalog.Images;
using Infrastructure.Catalog.Images;
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
            .Bind(configuration.GetSection(CloudinaryCatalogImageStorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<ICatalogImageStorage, CloudinaryCatalogImageStorage>();
        services.AddScoped<ICatalogAuthoringStore, CatalogAuthoringStore>();
        return services;
    }
}
