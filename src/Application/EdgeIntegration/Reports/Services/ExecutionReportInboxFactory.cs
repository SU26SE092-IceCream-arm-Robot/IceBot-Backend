using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.EdgeIntegration.CommandDelivery.Commands;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.EdgeIntegration.Reports.Commands;
using Application.EdgeIntegration.Timeouts.Commands;
using Application.EdgeIntegration.Dispatch.Contracts;
using Application.EdgeIntegration.Reports.Contracts;
using Domain.Sync.Ingestion;

namespace Application.EdgeIntegration.Reports.Services;

internal static class ExecutionReportInboxFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true
    };

    public static SyncEventInbox Create(
        IngestExecutionReportCommand command,
        Guid sourceExecutorId,
        DateTimeOffset cloudReceivedAt)
    {
        var payload = ToPayload(command);
        return new SyncEventInbox
        {
            EventId = command.SourceEventId,
            KioskId = command.KioskId,
            SourceNodeId = sourceExecutorId,
            CausationId = command.CommandId,
            EventType = $"ExecutionReport.{command.ReportType.Trim()}",
            AggregateType = "EdgeCommand",
            AggregateId = command.CommandId,
            PayloadJson = SerializeCanonical(payload),
            OccurredAt = command.EdgeCreatedAt,
            ReceivedAt = cloudReceivedAt
        };
    }

    public static bool HasSameIdentity(SyncEventInbox existing, SyncEventInbox candidate)
    {
        if (existing.KioskId != candidate.KioskId || existing.CausationId != candidate.CausationId ||
            existing.AggregateType != candidate.AggregateType || existing.AggregateId != candidate.AggregateId ||
            !string.Equals(existing.EventType, candidate.EventType, StringComparison.Ordinal))
            return false;

        return TryFingerprint(existing.PayloadJson, out var existingFingerprint) &&
               TryFingerprint(candidate.PayloadJson, out var candidateFingerprint) &&
               CryptographicOperations.FixedTimeEquals(existingFingerprint, candidateFingerprint);
    }

    private static ExecutionReportInboxPayload ToPayload(IngestExecutionReportCommand command) => new()
    {
        CommandId = command.CommandId,
        ReportType = command.ReportType.Trim(),
        Status = command.Status.Trim(),
        SequenceNumber = command.SequenceNumber,
        EdgeCreatedAt = command.EdgeCreatedAt,
        ExecutorReportedAt = command.ExecutorReportedAt,
        DeploymentId = command.DeploymentId,
        SourceProductionJobId = command.SourceProductionJobId,
        OrderItemId = command.OrderItemId,
        ProductionUnitNo = command.ProductionUnitNo,
        ProductionUnitQuantity = command.ProductionUnitQuantity,
        WorkcellId = command.WorkcellId,
        ControllerId = command.ControllerId,
        ExecutionPlanChecksum = command.ExecutionPlanChecksum,
        ActiveSetVersion = command.ActiveSetVersion,
        ActiveSetChecksum = command.ActiveSetChecksum,
        SourceConfigurationReleaseId = command.SourceConfigurationReleaseId,
        ReleaseChecksum = command.ReleaseChecksum,
        PhysicalOutputMayHaveOccurred = command.PhysicalOutputMayHaveOccurred,
        ErrorCode = command.ErrorCode,
        ErrorMessage = command.ErrorMessage,
        PayloadJson = command.PayloadJson,
        StockMovements = command.StockMovements.ToArray()
    };

    private static bool TryFingerprint(string? payloadJson, out byte[] fingerprint)
    {
        fingerprint = [];
        if (string.IsNullOrWhiteSpace(payloadJson)) return false;
        try
        {
            var payload = JsonSerializer.Deserialize<ExecutionReportInboxPayload>(payloadJson, JsonOptions);
            if (payload is null) return false;
            fingerprint = SHA256.HashData(Encoding.UTF8.GetBytes(SerializeCanonical(payload)));
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string SerializeCanonical(ExecutionReportInboxPayload payload) =>
        JsonSerializer.Serialize(payload, JsonOptions);
}
