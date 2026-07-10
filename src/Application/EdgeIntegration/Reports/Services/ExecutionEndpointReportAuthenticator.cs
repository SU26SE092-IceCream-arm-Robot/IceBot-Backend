using Application.EdgeIntegration.CommandDelivery.Commands;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.EdgeIntegration.Reports.Commands;
using Application.EdgeIntegration.Timeouts.Commands;
using Domain.Devices.Catalog;
using Domain.Devices.ExecutionEndpoints;

namespace Application.EdgeIntegration.Reports.Services;

internal static class ExecutionEndpointReportAuthenticator
{
    public static bool IsUsable(KioskExecutionEndpoint? endpoint, IngestExecutionReportCommand command) =>
        endpoint is not null && endpoint.KioskId == command.KioskId &&
        endpoint.Status == KioskExecutionEndpointStatus.Active && endpoint.CredentialBinding is not null &&
        endpoint.CredentialBinding.Status == ExecutionEndpointCredentialBindingStatus.Active;

    public static Guid? GetSourceExecutorId(KioskExecutionEndpoint endpoint) =>
        endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge ? endpoint.FullEdgeRuntimeId : endpoint.ControllerId;
}
