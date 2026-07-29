using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Infrastructure.Payments.Options;

namespace Infrastructure.Payments;

public static class PayOsHttpClientBuilderExtensions
{
    public static IHttpClientBuilder AddPayOsResilience(
        this IHttpClientBuilder builder,
        PayOsResilienceOptions settings)
    {
        builder.AddStandardResilienceHandler(options =>
        {
            options.Retry.DisableForUnsafeHttpMethods();
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(settings.AttemptTimeoutSeconds);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(settings.TotalTimeoutSeconds);
            options.CircuitBreaker.FailureRatio = settings.CircuitBreakerFailureRatio;
            options.CircuitBreaker.MinimumThroughput = settings.CircuitBreakerMinimumThroughput;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(settings.CircuitBreakerSamplingDurationSeconds);
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(settings.CircuitBreakerBreakDurationSeconds);
        });

        return builder;
    }
}
