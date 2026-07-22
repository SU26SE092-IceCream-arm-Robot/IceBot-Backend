using Application.RobotConfiguration.Storage.Services;
using Application.RobotConfiguration.Storage.Abstractions;
using Polly;
using Polly.Retry;

namespace Infrastructure.RobotConfiguration.Storage.ObjectStorage;

public sealed class ObjectStorageReadResiliencePipeline
{
    private readonly ResiliencePipeline _pipeline;

    public ObjectStorageReadResiliencePipeline(RobotArtifactObjectStorageOptions options)
    {
        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<IOException>()
                    .Handle<TimeoutException>(),
                MaxRetryAttempts = options.ReadRetryCount,
                Delay = TimeSpan.FromMilliseconds(options.ReadRetryDelayMilliseconds),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true
            })
            .Build();
    }

    public ValueTask ExecuteAsync(
        Func<CancellationToken, ValueTask> operation,
        CancellationToken cancellationToken = default) =>
        _pipeline.ExecuteAsync(operation, cancellationToken);

    public ValueTask<T> ExecuteAsync<T>(
        Func<CancellationToken, ValueTask<T>> operation,
        CancellationToken cancellationToken = default) =>
        _pipeline.ExecuteAsync(operation, cancellationToken);
}
