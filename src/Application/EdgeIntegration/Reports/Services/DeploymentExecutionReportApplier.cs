using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.CommandDelivery.Commands;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.EdgeIntegration.Reports.Commands;
using Application.EdgeIntegration.Timeouts.Commands;
using Domain.Common;
using Domain.Devices.Catalog;
using Domain.Devices.ExecutionEndpoints;
using Domain.Sync.Enums;

namespace Application.EdgeIntegration.Reports.Services;

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
        if (context.EdgeCommand.DeploymentId != command.DeploymentId)
            throw new DomainRuleException("Deployment report does not match the accepted command deployment.");

        if (endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge)
        {
            EnsureCommandTarget(context, DeploymentCommandTargetKind.FullEdgeConfiguration);
            var deployment = await store.GetFullEdgeDeploymentAsync(command.DeploymentId.Value, cancellationToken)
                ?? throw new DomainRuleException("Full Edge deployment not found.");
            EnsureOwnership(deployment.KioskId, deployment.KioskExecutionEndpointId, command);
            if (!ExecutionReportStatusMapper.Is(command.Status, "Failed"))
                EnsureFullEdgeProvenance(deployment.ConfigurationReleaseId, deployment.ReleaseChecksum, command);
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
            EnsureCommandTarget(context, DeploymentCommandTargetKind.LowCostArtifactSet);
            var deployment = await store.GetControllerArtifactSetDeploymentAsync(command.DeploymentId.Value, cancellationToken)
                ?? throw new DomainRuleException("Controller artifact-set deployment not found.");
            EnsureOwnership(deployment.KioskId, deployment.KioskExecutionEndpointId, command);
            if (!ExecutionReportStatusMapper.Is(command.Status, "Failed"))
            {
                EnsureLowCostProvenance(
                    deployment.SourceConfigurationReleaseId,
                    deployment.ReleaseChecksum,
                    deployment.ActiveSetVersion,
                    deployment.ActiveSetChecksum,
                    command);
            }
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

    private static void EnsureCommandTarget(
        ExecutionReportProcessingContext context,
        DeploymentCommandTargetKind expectedKind)
    {
        if (context.EdgeCommand.CommandType != EdgeCommandType.DeployConfiguration ||
            context.EdgeCommand.DeploymentKind != expectedKind)
        {
            throw new DomainRuleException("Deployment report does not match the accepted command target kind.");
        }
    }

    private static void EnsureFullEdgeProvenance(
        Guid releaseId,
        string releaseChecksum,
        IngestExecutionReportCommand command)
    {
        if (command.SourceConfigurationReleaseId != releaseId ||
            !string.Equals(command.ReleaseChecksum, releaseChecksum, StringComparison.Ordinal))
        {
            throw new DomainRuleException("Deployment report release provenance does not match the requested Full Edge deployment.");
        }
    }

    private static void EnsureLowCostProvenance(
        Guid releaseId,
        string releaseChecksum,
        long activeSetVersion,
        string activeSetChecksum,
        IngestExecutionReportCommand command)
    {
        if (command.SourceConfigurationReleaseId != releaseId ||
            !string.Equals(command.ReleaseChecksum, releaseChecksum, StringComparison.Ordinal) ||
            command.ActiveSetVersion != activeSetVersion ||
            !string.Equals(command.ActiveSetChecksum, activeSetChecksum, StringComparison.Ordinal))
        {
            throw new DomainRuleException("Deployment report provenance does not match the requested Low-cost artifact set.");
        }
    }

    private static string RequiredErrorCode(IngestExecutionReportCommand command) =>
        string.IsNullOrWhiteSpace(command.ErrorCode) ? "ExecutorReportedFailure" : command.ErrorCode.Trim();
}
