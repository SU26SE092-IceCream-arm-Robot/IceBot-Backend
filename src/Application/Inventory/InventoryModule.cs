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
        services.AddScoped<GetKioskInventoryTopologyQueryHandler>();
        services.AddScoped<RefillDispenserCommandHandler>();
        services.AddScoped<AdjustDispenserEstimateCommandHandler>();
        services.AddScoped<CreateDispenserStateCommandHandler>();
        services.AddScoped<UpdateDispenserStateCommandHandler>();
        services.AddScoped<SetDispenserStateStatusCommandHandler>();
        services.AddScoped<DeleteDispenserStateCommandHandler>();

        return services;
    }
}
