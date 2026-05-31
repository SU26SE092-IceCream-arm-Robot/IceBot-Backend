using Application.Orders.Abstractions;
using Infrastructure.Orders.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Orders;

public static class OrdersInfrastructureModule
{
    public static IServiceCollection AddOrdersInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IOrderStore, OrderStore>();
        return services;
    }
}
