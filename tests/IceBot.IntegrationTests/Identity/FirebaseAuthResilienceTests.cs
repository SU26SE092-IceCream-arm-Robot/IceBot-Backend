using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Infrastructure.Firebase;
using Microsoft.Extensions.Options;

namespace IceBot.IntegrationTests.Identity;

public sealed class FirebaseAuthResilienceTests
{
    [Theory]
    [InlineData(AuthErrorCode.InvalidIdToken)]
    [InlineData(AuthErrorCode.ExpiredIdToken)]
    [InlineData(AuthErrorCode.RevokedIdToken)]
    public void InvalidTokenErrorsAreNotRetryable(AuthErrorCode errorCode)
    {
        Assert.True(FirebaseAuthResiliencePipeline.IsInvalidToken(errorCode));
    }

    [Theory]
    [InlineData(ErrorCode.Unavailable)]
    [InlineData(ErrorCode.Internal)]
    [InlineData(ErrorCode.Unknown)]
    [InlineData(ErrorCode.DeadlineExceeded)]
    public void ExplicitServiceFailuresAreRetryable(ErrorCode errorCode)
    {
        Assert.True(FirebaseAuthResiliencePipeline.IsRetryable(errorCode));
    }

    [Fact]
    public async Task TransportFailureIsRetriedAndCanRecover()
    {
        var attempts = 0;
        var pipeline = CreatePipeline(retryCount: 1);

        var result = await pipeline.ExecuteAsync(_ =>
        {
            attempts++;
            return attempts == 1
                ? ValueTask.FromException<string>(new HttpRequestException("temporary transport failure"))
                : ValueTask.FromResult("verified");
        });

        Assert.Equal("verified", result);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task UnclassifiedFailureIsNotRetried()
    {
        var attempts = 0;
        var pipeline = CreatePipeline(retryCount: 2);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await pipeline.ExecuteAsync<string>(_ =>
            {
                attempts++;
                return ValueTask.FromException<string>(new InvalidOperationException("invalid token equivalent"));
            }));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task CallerCancellationStopsFirebaseRetryImmediately()
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

    private static FirebaseAuthResiliencePipeline CreatePipeline(int retryCount) =>
        new(Options.Create(new FirebaseAuthResilienceOptions
        {
            OperationTimeoutSeconds = 2,
            RetryCount = retryCount,
            RetryDelayMilliseconds = 1,
            CircuitBreakerFailureRatio = 1,
            CircuitBreakerMinimumThroughput = 100,
            CircuitBreakerSamplingDurationSeconds = 5,
            CircuitBreakerBreakDurationSeconds = 1
        }));
}
