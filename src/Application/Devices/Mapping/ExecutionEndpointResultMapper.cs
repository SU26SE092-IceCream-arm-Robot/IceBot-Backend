using Application.Devices.Results;
using Domain.Devices.Entities;
using Domain.Devices.Enums;

namespace Application.Devices.Mapping;

public static class ExecutionEndpointResultMapper
{
    public static ExecutionEndpointResult ToResult(KioskExecutionEndpoint endpoint)
    {
        return new ExecutionEndpointResult
        {
            Id = endpoint.Id,
            KioskId = endpoint.KioskId,
            KioskCode = endpoint.Kiosk.Code,
            EndpointCode = endpoint.EndpointCode,
            ExecutionProfile = endpoint.ExecutionProfile.ToString(),
            AuthenticationMode = endpoint.AuthenticationMode.ToString(),
            Status = endpoint.Status.ToString(),
            ProfileIdentity = endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
                ? endpoint.FullEdgeRuntimeId
                : endpoint.ControllerId,
            CredentialBindingId = endpoint.CredentialBindingId,
            CredentialStatus = endpoint.CredentialBinding?.Status.ToString(),
            ProvisionedAt = endpoint.ProvisionedAt,
            SupportedRobotTargets = endpoint.SupportedRobotTargets
                .OrderBy(target => target.RuntimeTargetCode)
                .ThenBy(target => target.MachineModelCode)
                .Select(target => new ExecutionEndpointRobotTargetResult
                {
                    Id = target.Id,
                    RuntimeTargetCode = target.RuntimeTargetCode,
                    MachineModelCode = target.MachineModelCode,
                    DeviceId = target.DeviceId,
                    DeviceCode = target.Device?.Code,
                    DeviceName = target.Device?.Name
                })
                .ToList()
        };
    }
}
