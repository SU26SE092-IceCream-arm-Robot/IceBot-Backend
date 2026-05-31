using Application.Catalog.Products.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Catalog;

public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        services.AddScoped<ProductManagementService>();
        return services;
    }
}
