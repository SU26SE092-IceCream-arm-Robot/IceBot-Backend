using Domain.Devices.ExecutionEndpoints;
using Application.Devices.Results;
using Domain.Devices.Entities;
using Domain.Devices.Enums;
using Domain.Devices.ExecutionEndpoints.Projections;

namespace Application.Devices.Mapping;

public static class ExecutionEndpointResultMapper
{
    public static ExecutionEndpointResult ToResult(
        KioskExecutionEndpoint endpoint,
        ExecutionEndpointReadinessProjection? readiness = null)
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
            MqttUsername = endpoint.MqttCredential?.Username,
            MqttCredentialStatus = endpoint.MqttCredential?.Status.ToString(),
            MqttCredentialVersion = endpoint.MqttCredential?.CredentialVersion,
            Readiness = readiness is null ? null : new ExecutionEndpointReadinessResult
            {
                StateRevision = readiness.StateRevision,
                Readiness = readiness.Readiness.ToString(),
                Activity = readiness.Activity.ToString(),
                Safety = readiness.Safety.ToString(),
                CurrentCommandId = readiness.CurrentCommandId,
                PhysicalOutputState = readiness.PhysicalOutputState.ToString(),
                FaultCode = readiness.FaultCode,
                ExecutorReportedAt = readiness.ExecutorReportedAt,
                Capabilities = readiness.Capabilities.OrderBy(x => x.CapabilityCode).Select(x => new ExecutionEndpointCapabilityResult
                {
                    CapabilityCode = x.CapabilityCode, WorkcellCode = x.WorkcellCode,
                    IsAvailable = x.IsAvailable, UnavailableReason = x.UnavailableReason
                }).ToArray()
            },
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
