using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Commands;
using Domain.Common;
using Domain.Devices.Enums;
using Domain.Devices.ExecutionEndpoints;

namespace Application.EdgeIntegration.Services;

internal static class DeploymentExecutionReportApplier
{
    public static async Task<bool> ApplyAsync(
        IExecutionReportUnitOfWork store,
        ExecutionReportProcessingContext context,
        CancellationToken cancellationToken)
    {
        var command = context.Command;
        var endpoint = context.Endpoint;
        var cloudReceivedAt = context.CloudReceivedAt;
        if (command.DeploymentId is null || command.DeploymentId == Guid.Empty)
            throw new DomainRuleException("Deployment reports require deployment id.");

        if (endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge)
        {
            var deployment = await store.GetFullEdgeDeploymentAsync(command.DeploymentId.Value, cancellationToken)
                ?? throw new DomainRuleException("Full Edge deployment not found.");
            EnsureOwnership(deployment.KioskId, deployment.KioskExecutionEndpointId, command);
            if (ExecutionReportStatusMapper.Is(command.Status, "Installed"))
                return deployment.MarkInstalled(command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt);
            if (ExecutionReportStatusMapper.Is(command.Status, "Active"))
            {
                var changed = deployment.MarkActive(command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt);
                endpoint.ApplyFullEdgeObservedActivation(deployment.Id, deployment.ConfigurationReleaseId,
                    deployment.ReleaseChecksum, command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt);
                return changed;
            }
            if (ExecutionReportStatusMapper.Is(command.Status, "Failed"))
                return deployment.MarkFailed(command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt,
                    RequiredErrorCode(command), command.ErrorMessage);
        }
        else
        {
            var deployment = await store.GetControllerArtifactSetDeploymentAsync(command.DeploymentId.Value, cancellationToken)
                ?? throw new DomainRuleException("Controller artifact-set deployment not found.");
            EnsureOwnership(deployment.KioskId, deployment.KioskExecutionEndpointId, command);
            if (ExecutionReportStatusMapper.Is(command.Status, "Installed"))
                return deployment.MarkInstalled(command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt);
            if (ExecutionReportStatusMapper.Is(command.Status, "Active"))
            {
                var changed = deployment.MarkActive(command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt);
                endpoint.ApplyLowCostObservedActivation(deployment.Id, deployment.SourceConfigurationReleaseId,
                    deployment.ReleaseChecksum, deployment.ActiveSetVersion, deployment.ActiveSetChecksum,
                    command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt);
                return changed;
            }
            if (ExecutionReportStatusMapper.Is(command.Status, "Failed"))
                return deployment.MarkFailed(command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt,
                    RequiredErrorCode(command), command.ErrorMessage);
        }

        throw new DomainRuleException("Unsupported deployment report status.");
    }

    private static void EnsureOwnership(Guid kioskId, Guid endpointId, IngestExecutionReportCommand command)
    {
        if (kioskId != command.KioskId || endpointId != command.EndpointId)
            throw new DomainRuleException("Deployment does not belong to the reporting endpoint.");
    }

    private static string RequiredErrorCode(IngestExecutionReportCommand command) =>
        string.IsNullOrWhiteSpace(command.ErrorCode) ? "ExecutorReportedFailure" : command.ErrorCode.Trim();
}
