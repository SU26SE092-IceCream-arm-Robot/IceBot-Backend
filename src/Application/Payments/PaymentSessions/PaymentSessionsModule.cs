using Application.Payments.PaymentSessions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Payments.PaymentSessions;

public static class PaymentSessionsModule
{
    public static IServiceCollection AddPaymentSessionsModule(this IServiceCollection services)
    {
        services.AddScoped<PaymentSessionService>();
        return services;
    }
}
