using Application.Orders.PlaceOrder.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Orders.PlaceOrder;

public static class PlaceOrderModule
{
    public static IServiceCollection AddPlaceOrderModule(this IServiceCollection services)
    {
        services.AddScoped<PlaceOrderService>();
        return services;
    }
}
