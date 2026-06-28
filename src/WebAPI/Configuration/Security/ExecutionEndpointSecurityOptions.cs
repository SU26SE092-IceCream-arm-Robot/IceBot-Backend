namespace WebAPI.Configuration.Security;

public sealed class ExecutionEndpointSecurityOptions
{
    public const string SectionName = "ExecutionEndpointSecurity";

    public int SignedRequestMaxClockSkewSeconds { get; init; } = 300;
    public int NonceRetentionSeconds { get; init; } = 900;
    public int MaxRequestBodyBytes { get; init; } = 1_048_576;
}
