using Application.Payments.PaymentSessions.Commands;
using Application.Payments.PaymentSessions.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Payments.PaymentSessions;

public static class PaymentSessionsModule
{
    public static IServiceCollection AddPaymentSessionsModule(this IServiceCollection services)
    {
        services.AddScoped<CreatePaymentSessionCommandHandler>();
        services.AddScoped<HandlePaymentProviderNotificationCommandHandler>();
        services.AddScoped<GetOrderPaymentStatusQueryHandler>();
        services.AddScoped<GetPaymentTransactionStatusQueryHandler>();
        return services;
    }
}
