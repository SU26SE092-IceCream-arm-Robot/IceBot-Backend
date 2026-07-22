using Application.Orders.PlaceOrder.Commands;
using Application.Orders.PlaceOrder.Services;
using Application.Orders.PlaceOrder.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Orders.PlaceOrder;

public static class PlaceOrderModule
{
    public static IServiceCollection AddPlaceOrderModule(this IServiceCollection services)
    {
        services.AddScoped<PlaceOrderCommandHandler>();
        services.AddScoped<PlaceOrderItemAppender>();
        services.AddScoped<GetOrderStatusQueryHandler>();
        services.AddScoped<CancelPendingOrderCommandHandler>();
        return services;
    }
}
