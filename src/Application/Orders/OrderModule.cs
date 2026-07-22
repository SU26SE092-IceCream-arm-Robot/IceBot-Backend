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
        services.AddScoped<Management.Queries.GetOrderStatusHistoryQueryHandler>();
        services.AddScoped<Management.Queries.GetOrderExecutionAttemptsQueryHandler>();
        services.AddScoped<Management.Queries.GetExecutionAttemptQueryHandler>();
        services.AddScoped<Management.Queries.GetOrderOverviewQueryHandler>();
        services.AddScoped<Management.Queries.ListFulfillmentQueueQueryHandler>();
        services.AddScoped<Management.Queries.GetOrderItemStatusHistoryQueryHandler>();
        services.AddScoped<Management.Commands.CancelManagementOrderCommandHandler>();
        services.AddScoped<Management.Commands.MarkOrderRefundRequiredCommandHandler>();
        services.AddScoped<Management.Commands.RedispatchOrderExecutionCommandHandler>();
        services.AddScoped<Management.Commands.RequestOrderItemProductionRemakeCommandHandler>();
        services.AddScoped<Management.Commands.RecordManualOrderItemFulfillmentEventCommandHandler>();
        services.AddScoped<Management.Commands.SetPackagedOrderItemFulfillmentCommandHandler>();
        services.AddScoped<Management.Automation.FulfillmentReminderService>();

        return services;
    }
}
