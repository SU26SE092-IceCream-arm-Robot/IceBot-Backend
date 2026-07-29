namespace Infrastructure.Payments.Options;

public sealed class PayOsResilienceOptions
{
    public const string SectionName = "PayOS:Resilience";

    public int AttemptTimeoutSeconds { get; set; } = 10;
    public int TotalTimeoutSeconds { get; set; } = 20;
    public double CircuitBreakerFailureRatio { get; set; } = 0.5;
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;
    public int CircuitBreakerSamplingDurationSeconds { get; set; } = 30;
    public int CircuitBreakerBreakDurationSeconds { get; set; } = 15;
}
