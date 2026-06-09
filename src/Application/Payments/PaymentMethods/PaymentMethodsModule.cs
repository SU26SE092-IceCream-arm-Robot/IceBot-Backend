using Application.Payments.PaymentMethods.Queries;
using Application.Payments.PaymentMethods.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Payments.PaymentMethods;

public static class PaymentMethodsModule
{
    public static IServiceCollection AddPaymentMethodsModule(this IServiceCollection services)
    {
        services.AddScoped<ListPaymentMethodsQueryHandler>();
        services.AddScoped<SetPaymentMethodStatusCommandHandler>();
        return services;
    }
}
