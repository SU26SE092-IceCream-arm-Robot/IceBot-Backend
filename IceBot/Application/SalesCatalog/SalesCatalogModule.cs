using Application.SalesCatalog.Menus.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.SalesCatalog;

public static class SalesCatalogModule
{
    public static IServiceCollection AddSalesCatalogModule(this IServiceCollection services)
    {
        services.AddScoped<MenuManagementService>();
        return services;
    }
}
