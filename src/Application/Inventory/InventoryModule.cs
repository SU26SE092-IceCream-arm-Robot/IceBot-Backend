using Application.Inventory.Commands;
using Application.Inventory.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Inventory;

public static class InventoryModule
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services)
    {
        services.AddScoped<GetDispenserStatesQueryHandler>();
        services.AddScoped<GetStockMovementsQueryHandler>();
        services.AddScoped<GetInventorySummaryQueryHandler>();
        services.AddScoped<RefillDispenserCommandHandler>();
        services.AddScoped<AdjustDispenserEstimateCommandHandler>();

        return services;
    }
}
