using Domain.Devices.ExecutionEndpoints;
namespace Domain.Devices.ExecutionEndpoints;

public enum ExecutionEndpointMqttCredentialStatus
{
    PendingProvision = 1,
    Active = 2,
    PendingRotation = 3,
    Failed = 4,
    Revoked = 5
}
