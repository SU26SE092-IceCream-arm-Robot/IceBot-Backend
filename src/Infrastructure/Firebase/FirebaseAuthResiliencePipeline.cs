using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Infrastructure.Firebase;

public sealed class FirebaseAuthResiliencePipeline
{
    private readonly ResiliencePipeline _pipeline;

    public FirebaseAuthResiliencePipeline(IOptions<FirebaseAuthResilienceOptions> options)
    {
        var settings = options.Value;
        var retryable = new PredicateBuilder()
            .Handle<HttpRequestException>()
            .Handle<IOException>()
            .Handle<TimeoutRejectedException>()
            .Handle<FirebaseAuthException>(IsRetryable);

        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = retryable,
                MaxRetryAttempts = settings.RetryCount,
                Delay = TimeSpan.FromMilliseconds(settings.RetryDelayMilliseconds),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = retryable,
                FailureRatio = settings.CircuitBreakerFailureRatio,
                MinimumThroughput = settings.CircuitBreakerMinimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(settings.CircuitBreakerSamplingDurationSeconds),
                BreakDuration = TimeSpan.FromSeconds(settings.CircuitBreakerBreakDurationSeconds)
            })
            .AddTimeout(TimeSpan.FromSeconds(settings.OperationTimeoutSeconds))
            .Build();
    }

    public ValueTask<T> ExecuteAsync<T>(
        Func<CancellationToken, ValueTask<T>> operation,
        CancellationToken cancellationToken = default) =>
        _pipeline.ExecuteAsync(operation, cancellationToken);

    public static bool IsRetryable(FirebaseAuthException exception) =>
        IsRetryable(exception.ErrorCode);

    public static bool IsRetryable(ErrorCode errorCode) =>
        errorCode is ErrorCode.Unavailable or
            ErrorCode.Internal or
            ErrorCode.Unknown or
            ErrorCode.DeadlineExceeded;

    public static bool IsInvalidToken(AuthErrorCode? errorCode) =>
        errorCode is AuthErrorCode.InvalidIdToken or
            AuthErrorCode.ExpiredIdToken or
            AuthErrorCode.RevokedIdToken;
}
