using Application.Catalog.Abstractions;
using Infrastructure.Catalog.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Catalog;

public static class CatalogInfrastructureModule
{
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IProductStore, ProductStore>();
        return services;
    }
}
