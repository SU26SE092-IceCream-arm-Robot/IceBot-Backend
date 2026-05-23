using Application.Payments.PaymentMethods.Interfaces;
using Application.Payments.PaymentMethods.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Payments.PaymentMethods;

public static class PaymentMethodsModule
{
    public static IServiceCollection AddPaymentMethodsModule(this IServiceCollection services)
    {
        services.AddScoped<IManagePaymentMethodService, ManagePaymentMethodService>();
        return services;
    }
}
