using Application.Orders.Abstractions;
using Application.Orders.Management.Abstractions;
using Application.Orders.Management.Automation;
using Application.Orders.IncidentResolution.Abstractions;
using Infrastructure.Orders.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Orders;

public static class OrdersInfrastructureModule
{
    public static IServiceCollection AddOrdersInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IOrderStore, OrderStore>();
        services.AddScoped<IOrderFulfillmentReadStore, OrderFulfillmentReadStore>();
        services.AddScoped<IFulfillmentReminderStore, FulfillmentReminderStore>();
        services.AddScoped<IProductionIncidentStore, ProductionIncidentStore>();
        return services;
    }
}
