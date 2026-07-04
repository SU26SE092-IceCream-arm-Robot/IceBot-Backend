namespace Infrastructure.Firebase;

public sealed class FirebaseAuthResilienceOptions
{
    public const string SectionName = "Firebase:Resilience";

    public int OperationTimeoutSeconds { get; set; } = 10;
    public int RetryCount { get; set; } = 1;
    public int RetryDelayMilliseconds { get; set; } = 200;
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;
    public int CircuitBreakerSamplingDurationSeconds { get; set; } = 30;
    public int CircuitBreakerBreakDurationSeconds { get; set; } = 15;
}
