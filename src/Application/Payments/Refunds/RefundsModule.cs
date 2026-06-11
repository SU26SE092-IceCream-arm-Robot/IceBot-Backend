using Application.Payments.Refunds.Commands;
using Application.Payments.Refunds.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Payments.Refunds;

public static class RefundsModule
{
    public static IServiceCollection AddRefundsModule(this IServiceCollection services)
    {
        services.AddScoped<ListManagementRefundsQueryHandler>();
        services.AddScoped<GetManagementRefundQueryHandler>();
        services.AddScoped<RequestRefundCommandHandler>();
        services.AddScoped<MarkRefundProcessedCommandHandler>();
        services.AddScoped<RejectRefundCommandHandler>();
        services.AddScoped<CancelRefundCommandHandler>();
        return services;
    }
}
