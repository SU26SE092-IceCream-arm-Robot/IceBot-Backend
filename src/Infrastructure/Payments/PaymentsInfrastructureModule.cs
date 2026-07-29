using Application.Payments.Abstractions;
using Infrastructure.Payments.Options;
using Infrastructure.Payments.Bootstrap;
using Infrastructure.Payments.Persistence;
using Infrastructure.Payments.Providers.PayOS;
using Infrastructure.Payments.Jobs;
using Application.Payments.PaymentSessions.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Application.Orders.PlaceOrder;

namespace Infrastructure.Payments;

public static class PaymentsInfrastructureModule
{
    public static IServiceCollection AddPaymentsInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<PayOsOptions>(config.GetSection(PayOsOptions.SectionName));
        var resilienceSettings = config.GetSection(PayOsResilienceOptions.SectionName)
            .Get<PayOsResilienceOptions>() ?? new PayOsResilienceOptions();
        services.AddOptions<PayOsResilienceOptions>()
            .Bind(config.GetSection(PayOsResilienceOptions.SectionName))
            .Validate(options =>
                    options.AttemptTimeoutSeconds is >= 1 and <= 120 &&
                    options.TotalTimeoutSeconds >= options.AttemptTimeoutSeconds &&
                    options.TotalTimeoutSeconds <= 300 &&
                    options.CircuitBreakerFailureRatio is > 0 and <= 1 &&
                    options.CircuitBreakerMinimumThroughput >= 2 &&
                    options.CircuitBreakerSamplingDurationSeconds >= options.AttemptTimeoutSeconds * 2 &&
                    options.CircuitBreakerBreakDurationSeconds >= 1,
                "PayOS resilience settings are invalid.")
            .ValidateOnStart();
        services.AddOptions<PaymentSessionReconciliationOptions>()
            .Bind(config.GetSection(PaymentSessionReconciliationOptions.SectionName))
            .Validate(options =>
                    options.IntervalSeconds is >= 5 and <= 3600 &&
                    options.StaleAfterSeconds is >= 5 and <= 3600 &&
                    options.RetryDelaySeconds is >= 5 and <= 3600 &&
                    options.BatchSize is >= 1 and <= 500,
                "Payment-session reconciliation settings are invalid.")
            .ValidateOnStart();
        services.AddOptions<OrderPaymentWindowOptions>()
            .Bind(config.GetSection(OrderPaymentWindowOptions.SectionName))
            .Validate(
                options => options.DurationMinutes is >= 1 and <= 120,
                "Order payment window must be between 1 and 120 minutes.")
            .ValidateOnStart();

        services.AddHttpClient<PayOsPaymentGateway>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<PayOsOptions>>().Value;
                client.BaseAddress = new Uri($"{options.BaseUrl.TrimEnd('/')}/", UriKind.Absolute);
                client.DefaultRequestHeaders.Add("x-client-id", options.ClientId);
                client.DefaultRequestHeaders.Add("x-api-key", options.ApiKey);
            })
            .AddPayOsResilience(resilienceSettings);

        services.AddScoped<IPaymentStore, PaymentStore>();
        services.AddScoped<IPaymentInterventionNotificationRecipientStore,
            PaymentInterventionNotificationRecipientStore>();
        services.AddScoped<IPaymentGateway>(provider => provider.GetRequiredService<PayOsPaymentGateway>());
        services.AddHostedService<PaymentMethodCatalogHostedService>();
        services.AddHostedService<PaymentSessionReconciliationJob>();

        return services;
    }
}
