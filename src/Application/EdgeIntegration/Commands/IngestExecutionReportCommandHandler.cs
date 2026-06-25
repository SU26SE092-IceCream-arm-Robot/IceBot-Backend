using System.Text.Json;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Results;
using Application.Shared.Wrappers;
using Domain.Common;
using Domain.Devices.Entities;
using Domain.Devices.Enums;
using Domain.ProductionExecution.Enums;
using Domain.ProductionExecution.Projections;
using Domain.Sync.Entities;
using Domain.Sync.Enums;

namespace Application.EdgeIntegration.Commands;

public sealed class IngestExecutionReportCommandHandler
{
    private readonly IExecutionReportStore _executionReportStore;

    public IngestExecutionReportCommandHandler(IExecutionReportStore executionReportStore)
    {
        _executionReportStore = executionReportStore;
    }

    public async Task<ApiResult<ExecutionReportIngestResult>> HandleAsync(
        IngestExecutionReportCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationError = Validate(command);
        if (validationError is not null)
        {
            return ApiResult<ExecutionReportIngestResult>.Fail(validationError, 400);
        }

        var endpoint = await _executionReportStore.GetEndpointForReportAuthAsync(command.EndpointId, cancellationToken);
        if (!IsAuthenticatedEndpoint(endpoint, command))
        {
            return ApiResult<ExecutionReportIngestResult>.Fail("Execution endpoint authentication failed.", 401);
        }

        var sourceExecutorId = GetSourceExecutorId(endpoint!);
        if (sourceExecutorId is null)
        {
            return ApiResult<ExecutionReportIngestResult>.Fail("Execution endpoint profile identity is missing.", 400);
        }

        var existingEvent = await _executionReportStore.GetSyncEventByEventIdAsync(command.SourceEventId, cancellationToken);
        if (existingEvent is not null)
        {
            return ApiResult<ExecutionReportIngestResult>.Success(
                BuildResult(command, applied: false, duplicate: true),
                "Execution report already ingested.");
        }

        var edgeCommand = await _executionReportStore.GetCommandAsync(command.CommandId, cancellationToken);
        if (edgeCommand is null ||
            edgeCommand.KioskId != command.KioskId ||
            edgeCommand.TargetExecutionEndpointId != command.EndpointId ||
            edgeCommand.Status != EdgeCommandStatus.Accepted)
        {
            return ApiResult<ExecutionReportIngestResult>.Fail("Accepted edge command not found for execution report.", 404);
        }

        var cloudReceivedAt = DateTimeOffset.UtcNow;
        var executorReportedAt = command.ExecutorReportedAt ?? command.EdgeCreatedAt;
        var inboxEvent = BuildInboxEvent(command, sourceExecutorId.Value, cloudReceivedAt);

        try
        {
            var applied = await ApplyReportAsync(
                command,
                endpoint!,
                sourceExecutorId.Value,
                edgeCommand,
                executorReportedAt,
                cloudReceivedAt,
                cancellationToken);

            inboxEvent.MarkProcessed(cloudReceivedAt);
            await _executionReportStore.AddSyncEventAsync(inboxEvent, cancellationToken);
            await _executionReportStore.SaveChangesAsync(cancellationToken);

            return ApiResult<ExecutionReportIngestResult>.Success(
                BuildResult(command, applied, duplicate: false),
                applied ? "Execution report applied successfully." : "Execution report accepted but did not change projection.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<ExecutionReportIngestResult>.Fail(ex.Message, 400);
        }
    }

    private static string? Validate(IngestExecutionReportCommand command)
    {
        if (command.KioskId == Guid.Empty ||
            command.EndpointId == Guid.Empty ||
            command.CommandId == Guid.Empty ||
            command.SourceEventId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.Credential) ||
            string.IsNullOrWhiteSpace(command.ReportType) ||
            string.IsNullOrWhiteSpace(command.Status) ||
            command.SequenceNumber <= 0)
        {
            return "Kiosk, endpoint, command, source event, sequence, report type, status, and credential are required.";
        }

        if (command.EdgeCreatedAt == default)
        {
            return "Edge-created timestamp is required.";
        }

        return null;
    }

    private static bool IsAuthenticatedEndpoint(KioskExecutionEndpoint? endpoint, IngestExecutionReportCommand command)
    {
        return endpoint is not null &&
            endpoint.KioskId == command.KioskId &&
            endpoint.Status == KioskExecutionEndpointStatus.Active &&
            endpoint.CredentialBinding is not null &&
            endpoint.CredentialBinding.Status == ExecutionEndpointCredentialBindingStatus.Active &&
            string.Equals(endpoint.CredentialBinding.CredentialReference, command.Credential.Trim(), StringComparison.Ordinal);
    }

    private static Guid? GetSourceExecutorId(KioskExecutionEndpoint endpoint)
    {
        return endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
            ? endpoint.FullEdgeRuntimeId
            : endpoint.ControllerId;
    }

    private async Task<bool> ApplyReportAsync(
        IngestExecutionReportCommand command,
        KioskExecutionEndpoint endpoint,
        Guid sourceExecutorId,
        EdgeCommand edgeCommand,
        DateTimeOffset executorReportedAt,
        DateTimeOffset cloudReceivedAt,
        CancellationToken cancellationToken)
    {
        if (string.Equals(command.ReportType, "Deployment", StringComparison.OrdinalIgnoreCase))
        {
            return await ApplyDeploymentReportAsync(command, endpoint, cloudReceivedAt, cancellationToken);
        }

        if (string.Equals(command.ReportType, "ProductionExecution", StringComparison.OrdinalIgnoreCase))
        {
            return await ApplyProductionExecutionReportAsync(
                command,
                endpoint,
                sourceExecutorId,
                edgeCommand,
                executorReportedAt,
                cloudReceivedAt,
                cancellationToken);
        }

        throw new DomainRuleException("Unsupported execution report type.");
    }

    private async Task<bool> ApplyDeploymentReportAsync(
        IngestExecutionReportCommand command,
        KioskExecutionEndpoint endpoint,
        DateTimeOffset cloudReceivedAt,
        CancellationToken cancellationToken)
    {
        if (command.DeploymentId is null || command.DeploymentId == Guid.Empty)
        {
            throw new DomainRuleException("Deployment reports require deployment id.");
        }

        if (endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge)
        {
            var deployment = await _executionReportStore.GetFullEdgeDeploymentAsync(command.DeploymentId.Value, cancellationToken)
                ?? throw new DomainRuleException("Full Edge deployment not found.");

            if (deployment.KioskId != command.KioskId || deployment.KioskExecutionEndpointId != command.EndpointId)
            {
                throw new DomainRuleException("Deployment does not belong to the reporting endpoint.");
            }

            if (IsStatus(command.Status, "Installed"))
            {
                return deployment.MarkInstalled(command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt);
            }

            if (IsStatus(command.Status, "Active"))
            {
                var changed = deployment.MarkActive(command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt);
                endpoint.ApplyFullEdgeObservedActivation(
                    deployment.Id,
                    deployment.ConfigurationReleaseId,
                    deployment.ReleaseChecksum,
                    command.SourceEventId,
                    command.EdgeCreatedAt,
                    cloudReceivedAt);
                return changed;
            }

            if (IsStatus(command.Status, "Failed"))
            {
                return deployment.MarkFailed(
                    command.SourceEventId,
                    command.EdgeCreatedAt,
                    cloudReceivedAt,
                    RequiredErrorCode(command),
                    command.ErrorMessage);
            }
        }
        else
        {
            var deployment = await _executionReportStore.GetControllerArtifactSetDeploymentAsync(command.DeploymentId.Value, cancellationToken)
                ?? throw new DomainRuleException("Controller artifact-set deployment not found.");

            if (deployment.KioskId != command.KioskId || deployment.KioskExecutionEndpointId != command.EndpointId)
            {
                throw new DomainRuleException("Deployment does not belong to the reporting endpoint.");
            }

            if (IsStatus(command.Status, "Installed"))
            {
                return deployment.MarkInstalled(command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt);
            }

            if (IsStatus(command.Status, "Active"))
            {
                var changed = deployment.MarkActive(command.SourceEventId, command.EdgeCreatedAt, cloudReceivedAt);
                endpoint.ApplyLowCostObservedActivation(
                    deployment.Id,
                    deployment.SourceConfigurationReleaseId,
                    deployment.ReleaseChecksum,
                    deployment.ActiveSetVersion,
                    deployment.ActiveSetChecksum,
                    command.SourceEventId,
                    command.EdgeCreatedAt,
                    cloudReceivedAt);
                return changed;
            }

            if (IsStatus(command.Status, "Failed"))
            {
                return deployment.MarkFailed(
                    command.SourceEventId,
                    command.EdgeCreatedAt,
                    cloudReceivedAt,
                    RequiredErrorCode(command),
                    command.ErrorMessage);
            }
        }

        throw new DomainRuleException("Unsupported deployment report status.");
    }

    private async Task<bool> ApplyProductionExecutionReportAsync(
        IngestExecutionReportCommand command,
        KioskExecutionEndpoint endpoint,
        Guid sourceExecutorId,
        EdgeCommand edgeCommand,
        DateTimeOffset executorReportedAt,
        DateTimeOffset cloudReceivedAt,
        CancellationToken cancellationToken)
    {
        if (edgeCommand.CommandType != EdgeCommandType.ExecuteOrder)
        {
            throw new DomainRuleException("Production execution reports require an execute-order command.");
        }

        var status = ParseProductionStatus(command.Status);
        var physicalOutputState = ToPhysicalOutputState(command.PhysicalOutputMayHaveOccurred);
        var productionRecord = await _executionReportStore.GetProductionExecutionRecordAsync(
            edgeCommand.Id,
            command.SourceProductionJobId,
            cancellationToken);

        var productionApplied = productionRecord is null;
        if (productionRecord is null)
        {
            productionRecord = ProductionExecutionRecord.Create(
                edgeCommand.Id,
                endpoint.Id,
                endpoint.ExecutionProfile,
                sourceExecutorId,
                command.SourceEventId,
                command.SequenceNumber,
                command.EdgeCreatedAt,
                executorReportedAt,
                cloudReceivedAt,
                status,
                physicalOutputState,
                command.SourceProductionJobId,
                command.WorkcellId,
                command.ControllerId,
                command.ExecutionPlanChecksum,
                command.ActiveSetVersion,
                command.ActiveSetChecksum,
                command.ErrorCode,
                command.ErrorMessage);

            await _executionReportStore.AddProductionExecutionRecordAsync(productionRecord, cancellationToken);
        }
        else
        {
            productionApplied = productionRecord.ApplyObservation(
                command.SourceEventId,
                command.SequenceNumber,
                command.EdgeCreatedAt,
                executorReportedAt,
                cloudReceivedAt,
                status,
                physicalOutputState,
                command.ErrorCode,
                command.ErrorMessage);
        }

        var orderApplied = await ApplyOrderExecutionRecordAsync(
            command,
            endpoint,
            sourceExecutorId,
            edgeCommand,
            executorReportedAt,
            cloudReceivedAt,
            status,
            cancellationToken);

        return productionApplied || orderApplied;
    }

    private async Task<bool> ApplyOrderExecutionRecordAsync(
        IngestExecutionReportCommand command,
        KioskExecutionEndpoint endpoint,
        Guid sourceExecutorId,
        EdgeCommand edgeCommand,
        DateTimeOffset executorReportedAt,
        DateTimeOffset cloudReceivedAt,
        ProductionExecutionStatus status,
        CancellationToken cancellationToken)
    {
        if (edgeCommand.OrderId is null || edgeCommand.DispatchAttemptNo is null)
        {
            return false;
        }

        if (command.SourceConfigurationReleaseId is null ||
            command.SourceConfigurationReleaseId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.ReleaseChecksum))
        {
            throw new DomainRuleException("Order execution reports require source configuration release and checksum.");
        }

        var customerStatus = MapCustomerStatus(status, command.PhysicalOutputMayHaveOccurred);
        var orderRecord = await _executionReportStore.GetOrderExecutionRecordAsync(edgeCommand.Id, cancellationToken);
        if (orderRecord is null)
        {
            orderRecord = OrderExecutionRecord.Create(
                edgeCommand.OrderId.Value,
                edgeCommand.Id,
                edgeCommand.DispatchAttemptNo.Value,
                endpoint.Id,
                endpoint.ExecutionProfile,
                sourceExecutorId,
                command.SourceConfigurationReleaseId.Value,
                command.ReleaseChecksum,
                command.SourceEventId,
                command.SequenceNumber,
                command.EdgeCreatedAt,
                executorReportedAt,
                cloudReceivedAt,
                status,
                ExecutionObservationStatus.Fresh,
                customerStatus);

            await _executionReportStore.AddOrderExecutionRecordAsync(orderRecord, cancellationToken);
            return true;
        }

        return orderRecord.ApplyObservation(
            command.SourceEventId,
            command.SequenceNumber,
            command.EdgeCreatedAt,
            executorReportedAt,
            cloudReceivedAt,
            status,
            ExecutionObservationStatus.Fresh,
            customerStatus);
    }

    private static SyncEventInbox BuildInboxEvent(
        IngestExecutionReportCommand command,
        Guid sourceExecutorId,
        DateTimeOffset cloudReceivedAt)
    {
        return new SyncEventInbox
        {
            EventId = command.SourceEventId,
            KioskId = command.KioskId,
            SourceNodeId = sourceExecutorId,
            CausationId = command.CommandId,
            EventType = $"ExecutionReport.{command.ReportType.Trim()}",
            AggregateType = "EdgeCommand",
            AggregateId = command.CommandId,
            PayloadJson = JsonSerializer.Serialize(new
            {
                command.CommandId,
                command.ReportType,
                command.Status,
                command.SequenceNumber,
                command.EdgeCreatedAt,
                command.ExecutorReportedAt,
                command.DeploymentId,
                command.SourceProductionJobId,
                command.WorkcellId,
                command.ControllerId,
                command.ExecutionPlanChecksum,
                command.ActiveSetVersion,
                command.ActiveSetChecksum,
                command.SourceConfigurationReleaseId,
                command.ReleaseChecksum,
                command.PhysicalOutputMayHaveOccurred,
                command.ErrorCode,
                command.ErrorMessage,
                command.PayloadJson
            }),
            OccurredAt = command.EdgeCreatedAt,
            ReceivedAt = cloudReceivedAt
        };
    }

    private static ExecutionReportIngestResult BuildResult(
        IngestExecutionReportCommand command,
        bool applied,
        bool duplicate)
    {
        return new ExecutionReportIngestResult
        {
            CommandId = command.CommandId,
            SourceEventId = command.SourceEventId,
            ReportType = command.ReportType.Trim(),
            Status = command.Status.Trim(),
            Applied = applied,
            Duplicate = duplicate
        };
    }

    private static bool IsStatus(string status, string expected)
    {
        return string.Equals(status.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }

    private static string RequiredErrorCode(IngestExecutionReportCommand command)
    {
        return string.IsNullOrWhiteSpace(command.ErrorCode) ? "ExecutorReportedFailure" : command.ErrorCode.Trim();
    }

    private static ProductionExecutionStatus ParseProductionStatus(string status)
    {
        return Enum.TryParse<ProductionExecutionStatus>(status.Trim(), ignoreCase: true, out var parsed)
            ? parsed
            : throw new DomainRuleException("Unsupported production execution status.");
    }

    private static PhysicalOutputState ToPhysicalOutputState(bool? physicalOutputMayHaveOccurred)
    {
        return physicalOutputMayHaveOccurred switch
        {
            true => PhysicalOutputState.Yes,
            false => PhysicalOutputState.No,
            _ => PhysicalOutputState.Unknown
        };
    }

    private static CustomerExecutionStatus MapCustomerStatus(
        ProductionExecutionStatus status,
        bool? physicalOutputMayHaveOccurred)
    {
        return status switch
        {
            ProductionExecutionStatus.Accepted or ProductionExecutionStatus.Running => CustomerExecutionStatus.Processing,
            ProductionExecutionStatus.Completed => CustomerExecutionStatus.Completed,
            ProductionExecutionStatus.Failed when physicalOutputMayHaveOccurred == true => CustomerExecutionStatus.SupportRequired,
            ProductionExecutionStatus.Failed => CustomerExecutionStatus.Failed,
            ProductionExecutionStatus.RequiresManualIntervention => CustomerExecutionStatus.SupportRequired,
            _ => CustomerExecutionStatus.ExecutionUnconfirmed
        };
    }
}
