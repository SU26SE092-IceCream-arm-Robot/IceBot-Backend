using Domain.Devices.ExecutionEndpoints;
namespace Domain.Devices.ExecutionEndpoints;

public enum ExecutionEndpointAuthenticationMode
{
    MutualTls = 1,
    SignedCommandTls = 2
}
