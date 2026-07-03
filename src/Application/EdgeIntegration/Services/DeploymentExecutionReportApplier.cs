using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Commands;
using Domain.Common;
using Domain.Devices.Enums;
using Domain.Devices.ExecutionEndpoints;

namespace Application.EdgeIntegration.Services;

internal static class DeploymentExecutionReportApplier
{
    public static async Task<bool> ApplyAsync(
        IDeploymentReportStore store,
        IngestExecutionReportCommand command,
        KioskExecutionEndpoint endpoint,
        DateTimeOffset cloudReceivedAt,
        CancellationToken cancellationToken)
    {
        if (command.DeploymentId is null || command.DeploymentId == Guid.Empty)
            throw new DomainRuleException("Deployment reports require deployment id.");

        if (endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge)
        {
            var deployment = await store.GetFullEdgeDeploymentAsync(command.DeploymentId.Value, cancellationToken)
                ?? throw new DomainRuleException("Full Edge deployment not found.");
            EnsureOwnership(deployment.KioskId, deployment.KioskExecutionEndpointId, command);
            if (ExecutionReportRules.IsStatus(command.Status, "Installed"))
                return deployment.MarkInstalled(command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt);
            if (ExecutionReportRules.IsStatus(command.Status, "Active"))
            {
                var changed = deployment.MarkActive(command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt);
                endpoint.ApplyFullEdgeObservedActivation(deployment.Id, deployment.ConfigurationReleaseId,
                    deployment.ReleaseChecksum, command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt);
                return changed;
            }
            if (ExecutionReportRules.IsStatus(command.Status, "Failed"))
                return deployment.MarkFailed(command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt,
                    ExecutionReportRules.RequiredErrorCode(command), command.ErrorMessage);
        }
        else
        {
            var deployment = await store.GetControllerArtifactSetDeploymentAsync(command.DeploymentId.Value, cancellationToken)
                ?? throw new DomainRuleException("Controller artifact-set deployment not found.");
            EnsureOwnership(deployment.KioskId, deployment.KioskExecutionEndpointId, command);
            if (ExecutionReportRules.IsStatus(command.Status, "Installed"))
                return deployment.MarkInstalled(command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt);
            if (ExecutionReportRules.IsStatus(command.Status, "Active"))
            {
                var changed = deployment.MarkActive(command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt);
                endpoint.ApplyLowCostObservedActivation(deployment.Id, deployment.SourceConfigurationReleaseId,
                    deployment.ReleaseChecksum, deployment.ActiveSetVersion, deployment.ActiveSetChecksum,
                    command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt);
                return changed;
            }
            if (ExecutionReportRules.IsStatus(command.Status, "Failed"))
                return deployment.MarkFailed(command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt,
                    ExecutionReportRules.RequiredErrorCode(command), command.ErrorMessage);
        }

        throw new DomainRuleException("Unsupported deployment report status.");
    }

    private static void EnsureOwnership(Guid kioskId, Guid endpointId, IngestExecutionReportCommand command)
    {
        if (kioskId != command.KioskId || endpointId != command.EndpointId)
            throw new DomainRuleException("Deployment does not belong to the reporting endpoint.");
    }
}
