using Application.SalesCatalog.Abstractions;
using Infrastructure.SalesCatalog.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.SalesCatalog;

public static class SalesCatalogInfrastructureModule
{
    public static IServiceCollection AddSalesCatalogInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IMenuStore, MenuStore>();
        return services;
    }
}
