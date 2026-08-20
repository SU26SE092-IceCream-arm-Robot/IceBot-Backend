using Application.Inventory.Commands;
using Application.Inventory.Queries;
using Application.Inventory.Abstractions;
using Application.Inventory.Observations;
using Application.Inventory.Services;
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
        services.AddScoped<GetDispenserRebindHistoryQueryHandler>();
        services.AddScoped<GetDispenserHistoryQueryHandler>();
        services.AddScoped<GetKioskIngredientInventoriesQueryHandler>();
        services.AddScoped<GetKioskInventoryWorkspaceQueryHandler>();
        services.AddScoped<ListInventoryRefillTasksQueryHandler>();
        services.AddScoped<GetInventoryRefillTaskQueryHandler>();
        services.AddScoped<IInventoryReadinessEvaluator, InventoryReadinessEvaluator>();
        services.AddScoped<CreateDispenserStateCommandHandler>();
        services.AddScoped<UpdateDispenserStateCommandHandler>();
        services.AddScoped<SetDispenserStateStatusCommandHandler>();
        services.AddScoped<DeleteDispenserStateCommandHandler>();
        services.AddScoped<RebindDispenserStateCommandHandler>();
        services.AddScoped<IngestInventorySensorObservationsCommandHandler>();
        services.AddScoped<CreateKioskIngredientInventoryCommandHandler>();
        services.AddScoped<UpdateKioskIngredientInventoryCommandHandler>();
        services.AddScoped<AdjustKioskIngredientInventoryCommandHandler>();
        services.AddScoped<RequestInventoryRefillTaskCommandHandler>();
        services.AddScoped<StartInventoryRefillTaskCommandHandler>();
        services.AddScoped<CompleteInventoryRefillTaskCommandHandler>();
        services.AddScoped<CancelInventoryRefillTaskCommandHandler>();

        return services;
    }
}
