using Microsoft.Extensions.Options;
using Polly;

namespace Infrastructure.Firebase;

// Delivery retry remains owned by the durable notification-delivery job.
public sealed class FirebasePushDeliveryTimeoutPolicy
{
    private readonly ResiliencePipeline _pipeline;

    public FirebasePushDeliveryTimeoutPolicy(IOptions<FirebasePushDeliveryOptions> options)
    {
        _pipeline = new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromSeconds(options.Value.OperationTimeoutSeconds))
            .Build();
    }

    public ValueTask<T> ExecuteAsync<T>(
        Func<CancellationToken, ValueTask<T>> operation,
        CancellationToken cancellationToken = default) =>
        _pipeline.ExecuteAsync(operation, cancellationToken);
}
