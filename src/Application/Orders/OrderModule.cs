using Application.Orders.PlaceOrder;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Orders;

public static class OrderModule
{
    public static IServiceCollection AddOrderModule(this IServiceCollection services)
    {
        services.AddPlaceOrderModule();
        
        services.AddScoped<Management.Queries.ListManagementOrdersQueryHandler>();
        services.AddScoped<Management.Queries.GetManagementOrderQueryHandler>();
        services.AddScoped<Management.Commands.CancelManagementOrderCommandHandler>();
        services.AddScoped<Management.Commands.MarkOrderRefundRequiredCommandHandler>();

        return services;
    }
}
