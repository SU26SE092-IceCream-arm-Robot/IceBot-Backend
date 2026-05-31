using Application.Payments.PaymentMethods;
using Application.Payments.PaymentSessions;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Payments;

public static class PaymentModule
{
    public static IServiceCollection AddPaymentModule(this IServiceCollection services)
    {
        services.AddPaymentMethodsModule();
        services.AddPaymentSessionsModule();
        return services;
    }
}
