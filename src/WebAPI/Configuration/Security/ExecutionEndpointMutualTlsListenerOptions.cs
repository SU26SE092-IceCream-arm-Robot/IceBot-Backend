namespace WebAPI.Configuration.Security;

public sealed class ExecutionEndpointMutualTlsListenerOptions
{
    public const string SectionName = "ExecutionEndpointTransport:MutualTlsListener";

    // Enable only when the deployment exposes the dedicated Edge HTTPS endpoint.
    public bool Required { get; init; }

    public string EndpointName { get; init; } = "EdgeMtls";
}
