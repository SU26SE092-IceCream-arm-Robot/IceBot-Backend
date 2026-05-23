using Application.Orders.PlaceOrder;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Orders;

public static class OrderModule
{
    public static IServiceCollection AddOrderModule(this IServiceCollection services)
    {
        services.AddPlaceOrderModule();
        return services;
    }
}
