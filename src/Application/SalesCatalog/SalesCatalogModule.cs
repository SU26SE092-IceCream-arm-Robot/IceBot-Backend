using Application.SalesCatalog.Menus.Commands;
using Application.SalesCatalog.Menus.Queries;
using Application.SalesCatalog.RuntimeMenus.Queries;
using Application.SalesCatalog.RuntimeMenus.Services;
using Application.SalesCatalog.Availability;
using Microsoft.Extensions.DependencyInjection;

namespace Application.SalesCatalog;

public static class SalesCatalogModule
{
    public static IServiceCollection AddSalesCatalogModule(this IServiceCollection services)
    {
        services.AddScoped<ListMenusQueryHandler>();
        services.AddScoped<GetMenuQueryHandler>();
        services.AddScoped<CreateMenuCommandHandler>();
        services.AddScoped<UpdateMenuCommandHandler>();
        services.AddScoped<SetMenuStatusCommandHandler>();
        services.AddScoped<DeleteMenuCommandHandler>();
        services.AddScoped<AddMenuItemCommandHandler>();
        services.AddScoped<UpdateMenuItemCommandHandler>();
        services.AddScoped<SetMenuItemStatusCommandHandler>();
        services.AddScoped<DeleteMenuItemCommandHandler>();
        services.AddScoped<ListKioskMenuItemAvailabilityQueryHandler>();
        services.AddScoped<SetKioskMenuItemAvailabilityCommandHandler>();
        services.AddScoped<IMenuItemOperationalAvailabilityReader, MenuItemOperationalAvailabilityReader>();
        services.AddScoped<MachineProductionInventoryGate>();

        services.AddScoped<RuntimeMenuProjectionBuilder>();
        services.AddScoped<GetKioskRuntimeMenuQueryHandler>();
        return services;
    }
}
