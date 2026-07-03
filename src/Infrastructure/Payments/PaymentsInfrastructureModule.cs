using Application.Payments.Abstractions;
using Infrastructure.Payments.Options;
using Infrastructure.Payments.Persistence;
using Infrastructure.Payments.Providers.PayOS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace Infrastructure.Payments;

public static class PaymentsInfrastructureModule
{
    public static IServiceCollection AddPaymentsInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<PayOsOptions>(config.GetSection(PayOsOptions.SectionName));

        services.AddHttpClient<PayOsPaymentGateway>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<PayOsOptions>>().Value;
                client.BaseAddress = new Uri($"{options.BaseUrl.TrimEnd('/')}/", UriKind.Absolute);
                client.DefaultRequestHeaders.Add("x-client-id", options.ClientId);
                client.DefaultRequestHeaders.Add("x-api-key", options.ApiKey);
            })
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.DisableForUnsafeHttpMethods();
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);
            });

        services.AddScoped<IPaymentStore, PaymentStore>();
        services.AddScoped<IPaymentGateway>(provider => provider.GetRequiredService<PayOsPaymentGateway>());

        return services;
    }
}
