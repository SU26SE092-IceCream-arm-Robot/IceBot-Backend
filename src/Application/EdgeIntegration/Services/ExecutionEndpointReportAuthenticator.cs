using Application.EdgeIntegration.Commands;
using Domain.Devices.Enums;
using Domain.Devices.ExecutionEndpoints;

namespace Application.EdgeIntegration.Services;

internal static class ExecutionEndpointReportAuthenticator
{
    public static bool IsUsable(KioskExecutionEndpoint? endpoint, IngestExecutionReportCommand command) =>
        endpoint is not null && endpoint.KioskId == command.KioskId &&
        endpoint.Status == KioskExecutionEndpointStatus.Active && endpoint.CredentialBinding is not null &&
        endpoint.CredentialBinding.Status == ExecutionEndpointCredentialBindingStatus.Active;

    public static Guid? GetSourceExecutorId(KioskExecutionEndpoint endpoint) =>
        endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge ? endpoint.FullEdgeRuntimeId : endpoint.ControllerId;
}
