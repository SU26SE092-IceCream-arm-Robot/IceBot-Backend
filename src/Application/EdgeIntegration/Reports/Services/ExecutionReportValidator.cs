using Application.EdgeIntegration.CommandDelivery.Commands;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.EdgeIntegration.Reports.Commands;
using Application.EdgeIntegration.Timeouts.Commands;

namespace Application.EdgeIntegration.Reports.Services;

internal static class ExecutionReportValidator
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
        var hasJobIdentity = command.SourceProductionJobId.HasValue || command.OrderItemId.HasValue ||
            command.ProductionUnitNo.HasValue || command.ProductionUnitQuantity.HasValue;
        if (hasJobIdentity &&
            !string.Equals(command.ReportType, "ProductionExecution", StringComparison.OrdinalIgnoreCase))
            return "Production-job identity is supported only for production execution reports.";
        if (hasJobIdentity && (!command.SourceProductionJobId.HasValue || !command.OrderItemId.HasValue ||
                command.OrderItemId == Guid.Empty || command.ProductionUnitNo is null or <= 0 ||
                command.ProductionUnitQuantity is null or <= 0))
            return "Production-job reports require source job, order item, positive unit number, and positive unit quantity.";
        if (command.StockMovements.Count > 100)
            return "A production execution report supports at most 100 stock movement evidence items.";
        if (command.StockMovements.Any(item => item.OrderItemId != command.OrderItemId))
            return "Stock movement evidence must belong to the reported production-job order item.";
        if (command.StockMovements.Any(item => item.SourceEventId == Guid.Empty || item.OrderItemId == Guid.Empty ||
                item.IngredientDispenserStateId == Guid.Empty || item.QuantityConsumed <= 0 || item.BalanceAfter < 0) ||
            command.StockMovements.Select(item => item.SourceEventId).Distinct().Count() != command.StockMovements.Count)
            return "Stock movement evidence requires unique event ids, dispenser states, positive consumed quantities, and non-negative balances.";
        if (string.Equals(command.ReportType, "ProductionExecution", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(command.Status, "Failed", StringComparison.OrdinalIgnoreCase) &&
            !command.PhysicalOutputMayHaveOccurred.HasValue)
            return "Failed production execution reports must state whether physical output may have occurred.";
        return null;
    }
}
