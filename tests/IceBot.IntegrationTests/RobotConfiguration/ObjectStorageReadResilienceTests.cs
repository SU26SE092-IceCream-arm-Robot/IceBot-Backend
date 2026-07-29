using Application.RobotConfiguration.Storage.Services;
using Application.RobotConfiguration.Storage.Abstractions;
using Infrastructure.RobotConfiguration.Storage.ObjectStorage;

namespace IceBot.IntegrationTests.RobotConfiguration;

public sealed class ObjectStorageReadResilienceTests
{
    [Fact]
    public async Task TransientTimeoutIsRetriedAndCanRecover()
    {
        var attempts = 0;
        var pipeline = CreatePipeline(retryCount: 1);

        var result = await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            return attempts == 1
                ? ValueTask.FromException<string>(new TimeoutException("temporary timeout"))
                : ValueTask.FromResult("ok");
        });

        Assert.Equal("ok", result);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task BusinessFailureIsNotRetried()
    {
        var attempts = 0;
        var pipeline = CreatePipeline(retryCount: 2);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await pipeline.ExecuteAsync<string>(_ =>
            {
                attempts++;
                return ValueTask.FromException<string>(new InvalidOperationException("bucket missing"));
            }));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task CallerCancellationStopsStorageRetryImmediately()
    {
        using var cancellation = new CancellationTokenSource();
        var attempts = 0;
        var pipeline = CreatePipeline(retryCount: 3);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await pipeline.ExecuteAsync<string>(_ =>
            {
                attempts++;
                cancellation.Cancel();
                return ValueTask.FromCanceled<string>(cancellation.Token);
            }, cancellation.Token));

        Assert.Equal(1, attempts);
    }

    private static ObjectStorageReadResiliencePipeline CreatePipeline(int retryCount) =>
        new(new RobotArtifactObjectStorageOptions
        {
            ReadRetryCount = retryCount,
            ReadRetryDelayMilliseconds = 1
        });
}
