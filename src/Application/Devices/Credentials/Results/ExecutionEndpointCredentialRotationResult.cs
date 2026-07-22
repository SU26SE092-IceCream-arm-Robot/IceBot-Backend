using Domain.Devices.ExecutionEndpoints;
using Domain.Devices.Catalog;

namespace Application.Devices.Credentials.Results;

public sealed class ExecutionEndpointCredentialRotationResult
{
    public Guid EndpointId { get; init; }
    public Guid KioskId { get; init; }
    public string ExecutionProfile { get; init; } = null!;
    public string AuthenticationMode { get; init; } = null!;
    public string Status { get; init; } = null!;

    public static ExecutionEndpointCredentialRotationResult FromEndpoint(KioskExecutionEndpoint endpoint)
    {
        return new ExecutionEndpointCredentialRotationResult
        {
            EndpointId = endpoint.Id,
            KioskId = endpoint.KioskId,
            ExecutionProfile = endpoint.ExecutionProfile.ToString(),
            AuthenticationMode = endpoint.AuthenticationMode.ToString(),
            Status = endpoint.Status.ToString()
        };
    }
}
