using Application.Payments.Abstractions;
using Infrastructure.Payments.Options;
using Infrastructure.Payments.Persistence;
using Infrastructure.Payments.Providers.PayOS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Payments;

public static class PaymentsInfrastructureModule
{
    public static IServiceCollection AddPaymentsInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient();

        services.Configure<PayOsOptions>(config.GetSection(PayOsOptions.SectionName));

        services.AddScoped<IPaymentStore, PaymentStore>();
        services.AddScoped<IPaymentGateway, PayOsPaymentGateway>();

        return services;
    }
}
