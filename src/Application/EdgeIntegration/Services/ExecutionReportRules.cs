using System.Text.Json;
using Application.EdgeIntegration.Commands;
using Application.EdgeIntegration.Contracts;
using Application.EdgeIntegration.Results;
using Domain.Common;
using Domain.Devices.ExecutionEndpoints;
using Domain.Devices.Enums;
using Domain.ProductionExecution.Enums;
using Domain.Sync.Entities;
using Domain.Sync.Ingestion;

namespace Application.EdgeIntegration.Services;

internal static class ExecutionReportRules
{
    public static string? Validate(IngestExecutionReportCommand command, DateTimeOffset receivedAt, int maxFutureClockSkewSeconds)
    {
        if (command.KioskId == Guid.Empty || command.EndpointId == Guid.Empty || command.CommandId == Guid.Empty ||
            command.SourceEventId == Guid.Empty || string.IsNullOrWhiteSpace(command.ReportType) ||
            string.IsNullOrWhiteSpace(command.Status) || command.SequenceNumber <= 0)
            return "Kiosk, endpoint, command, source event, sequence, report type, and status are required.";
        if (command.EdgeCreatedAt == default) return "Edge-created timestamp is required.";

        var latestAcceptedTimestamp = receivedAt.AddSeconds(maxFutureClockSkewSeconds);
        if (command.EdgeCreatedAt > latestAcceptedTimestamp || command.ExecutorReportedAt > latestAcceptedTimestamp ||
            command.StockMovements.Any(item => item.OccurredAt > latestAcceptedTimestamp))
            return "Execution report timestamps cannot exceed the allowed future clock skew.";
        if (command.StockMovements.Count > 0 &&
            !string.Equals(command.ReportType, "ProductionExecution", StringComparison.OrdinalIgnoreCase))
            return "Stock movement evidence is supported only for production execution reports.";
        if (command.StockMovements.Count > 0 && !command.SourceProductionJobId.HasValue)
            return "Stock movement evidence must be reported by a production job.";
        if (command.StockMovements.Count > 100)
            return "A production execution report supports at most 100 stock movement evidence items.";
        if (command.StockMovements.Any(item => item.SourceEventId == Guid.Empty ||
                item.IngredientDispenserStateId == Guid.Empty || item.QuantityConsumed <= 0 || item.BalanceAfter < 0) ||
            command.StockMovements.Select(item => item.SourceEventId).Distinct().Count() != command.StockMovements.Count)
            return "Stock movement evidence requires unique event ids, dispenser states, positive consumed quantities, and non-negative balances.";
        return null;
    }

    public static bool IsUsableEndpoint(KioskExecutionEndpoint? endpoint, IngestExecutionReportCommand command) =>
        endpoint is not null && endpoint.KioskId == command.KioskId &&
        endpoint.Status == KioskExecutionEndpointStatus.Active && endpoint.CredentialBinding is not null &&
        endpoint.CredentialBinding.Status == ExecutionEndpointCredentialBindingStatus.Active;

    public static Guid? GetSourceExecutorId(KioskExecutionEndpoint endpoint) =>
        endpoint.ExecutionProfile == KioskExecutionProfile.FullEdge ? endpoint.FullEdgeRuntimeId : endpoint.ControllerId;

    public static SyncEventInbox BuildInboxEvent(
        IngestExecutionReportCommand command, Guid sourceExecutorId, DateTimeOffset cloudReceivedAt) => new()
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
            command.CommandId, command.ReportType, command.Status, command.SequenceNumber, command.EdgeCreatedAt,
            command.ExecutorReportedAt, command.DeploymentId, command.SourceProductionJobId, command.WorkcellId,
            command.ControllerId, command.ExecutionPlanChecksum, command.ActiveSetVersion, command.ActiveSetChecksum,
            command.SourceConfigurationReleaseId, command.ReleaseChecksum, command.PhysicalOutputMayHaveOccurred,
            command.ErrorCode, command.ErrorMessage, command.PayloadJson, command.StockMovements
        }),
        OccurredAt = command.EdgeCreatedAt,
        ReceivedAt = cloudReceivedAt
    };

    public static bool MatchesIdentity(SyncEventInbox existing, SyncEventInbox candidate) =>
        existing.KioskId == candidate.KioskId && existing.CausationId == candidate.CausationId &&
        existing.AggregateType == candidate.AggregateType && existing.AggregateId == candidate.AggregateId &&
        string.Equals(existing.EventType, candidate.EventType, StringComparison.Ordinal) &&
        string.Equals(existing.PayloadJson, candidate.PayloadJson, StringComparison.Ordinal);

    public static ExecutionReportIngestResult BuildResult(IngestExecutionReportCommand command, bool applied, bool duplicate) => new()
    {
        CommandId = command.CommandId,
        SourceEventId = command.SourceEventId,
        ReportType = command.ReportType.Trim(),
        Status = command.Status.Trim(),
        Applied = applied,
        Duplicate = duplicate
    };

    public static bool IsStatus(string status, string expected) =>
        string.Equals(status.Trim(), expected, StringComparison.OrdinalIgnoreCase);

    public static string RequiredErrorCode(IngestExecutionReportCommand command) =>
        string.IsNullOrWhiteSpace(command.ErrorCode) ? "ExecutorReportedFailure" : command.ErrorCode.Trim();

    public static ProductionExecutionStatus ParseProductionStatus(string status) =>
        Enum.TryParse<ProductionExecutionStatus>(status.Trim(), true, out var parsed)
            ? parsed
            : throw new DomainRuleException("Unsupported production execution status.");

    public static PhysicalOutputState ToPhysicalOutputState(bool? value) => value switch
    {
        true => PhysicalOutputState.Yes,
        false => PhysicalOutputState.No,
        _ => PhysicalOutputState.Unknown
    };

    public static CustomerExecutionStatus MapCustomerStatus(ProductionExecutionStatus status, bool? physicalOutput) => status switch
    {
        ProductionExecutionStatus.Accepted or ProductionExecutionStatus.Running => CustomerExecutionStatus.Processing,
        ProductionExecutionStatus.Completed => CustomerExecutionStatus.Completed,
        ProductionExecutionStatus.Failed when physicalOutput == true => CustomerExecutionStatus.SupportRequired,
        ProductionExecutionStatus.Failed => CustomerExecutionStatus.Failed,
        ProductionExecutionStatus.RequiresManualIntervention => CustomerExecutionStatus.SupportRequired,
        _ => CustomerExecutionStatus.ExecutionUnconfirmed
    };

    public static void ValidateRelease(IngestExecutionReportCommand command, EdgeCommand edgeCommand)
    {
        if (!command.SourceConfigurationReleaseId.HasValue || command.SourceConfigurationReleaseId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.ReleaseChecksum))
            throw new DomainRuleException("Production execution reports require source configuration release and checksum.");

        var payload = ExecuteOrderCommandPayloadCodec.Deserialize(edgeCommand.PayloadJson);
        if (command.SourceConfigurationReleaseId.Value != payload.ConfigurationReleaseId ||
            !string.Equals(command.ReleaseChecksum.Trim(), payload.ReleaseChecksum, StringComparison.Ordinal))
            throw new DomainRuleException("Production execution report release does not match the dispatched command.");
    }
}
