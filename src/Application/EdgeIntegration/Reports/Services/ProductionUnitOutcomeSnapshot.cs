using Domain.Common;
using Domain.ProductionExecution.Enums;
using Domain.ProductionExecution.Projections;
using Domain.Sync.Entities;
using Application.EdgeIntegration.Dispatch.Contracts;

namespace Application.EdgeIntegration.Reports.Services;

internal sealed class ProductionUnitOutcomeSnapshot
{
    private ProductionUnitOutcomeSnapshot(
        int firstUnitNo,
        int expectedQuantity,
        IReadOnlyList<ProductionExecutionRecord?> units)
    {
        FirstUnitNo = firstUnitNo;
        ExpectedQuantity = expectedQuantity;
        Units = units;
    }

    public int FirstUnitNo { get; }
    public int ExpectedQuantity { get; }
    public IReadOnlyList<ProductionExecutionRecord?> Units { get; }
    public bool HasEvidence => Units.Any(record => record is not null);
    public bool HasCompleteCoverage => Units.All(record => record is not null);
    public int CompletedQuantity => Count(ProductionExecutionStatus.Completed);
    public int FailedQuantity => Count(ProductionExecutionStatus.Failed);
    public int ManualInterventionQuantity => Count(ProductionExecutionStatus.RequiresManualIntervention);
    public int InProgressQuantity => Count(ProductionExecutionStatus.Accepted) + Count(ProductionExecutionStatus.Running);
    public int UnreportedQuantity => Units.Count(record => record is null);

    public ProductionExecutionStatus? AggregateStatus
    {
        get
        {
            if (!HasEvidence) return null;
            if (FailedQuantity > 0) return ProductionExecutionStatus.Failed;
            if (ManualInterventionQuantity > 0) return ProductionExecutionStatus.RequiresManualIntervention;
            if (HasCompleteCoverage && CompletedQuantity == ExpectedQuantity)
                return ProductionExecutionStatus.Completed;
            if (Units.Any(record => record?.Status is ProductionExecutionStatus.Running or ProductionExecutionStatus.Completed))
                return ProductionExecutionStatus.Running;
            return ProductionExecutionStatus.Accepted;
        }
    }

    public static ProductionUnitOutcomeSnapshot Create(
        int expectedQuantity,
        IReadOnlyCollection<ProductionExecutionRecord> records,
        int firstUnitNo = 1)
    {
        if (expectedQuantity <= 0 || firstUnitNo <= 0)
            throw new DomainRuleException("Production order-line quantity must be greater than zero.");

        var units = new ProductionExecutionRecord?[expectedQuantity];
        var expectedLastUnit = checked(firstUnitNo + expectedQuantity - 1);
        foreach (var record in records)
        {
            var lastUnit = checked(record.ProductionUnitNo + record.ProductionUnitQuantity - 1);
            if (record.ProductionUnitNo < firstUnitNo || lastUnit > expectedLastUnit)
                throw new DomainRuleException("Production job unit range exceeds the dispatched order-line quantity.");

            for (var unitNo = record.ProductionUnitNo; unitNo <= lastUnit; unitNo++)
            {
                var index = unitNo - firstUnitNo;
                if (units[index] is not null &&
                    units[index]!.SourceProductionJobId != record.SourceProductionJobId)
                {
                    throw new DomainRuleException(
                        $"Production job unit range overlaps unit {unitNo} for the same order item and command.");
                }

                units[index] = record;
            }
        }

        return new ProductionUnitOutcomeSnapshot(firstUnitNo, expectedQuantity, units);
    }

    public static ProductionUnitOutcomeSnapshot CreateEffective(
        int expectedQuantity,
        IReadOnlyCollection<ProductionExecutionRecord> records,
        EdgeCommand currentCommand)
    {
        if (expectedQuantity <= 0)
            throw new DomainRuleException("Production order-line quantity must be greater than zero.");

        var units = new ProductionExecutionRecord?[expectedQuantity];
        var attempts = new int[expectedQuantity];
        foreach (var record in records.OrderBy(record =>
                     ResolveCommand(record, currentCommand).DispatchAttemptNo ?? 0))
        {
            var command = ResolveCommand(record, currentCommand);
            var payload = ExecuteOrderCommandPayloadCodec.DeserializeAndValidateFull(command.PayloadJson);
            var line = payload.OrderLines.SingleOrDefault(candidate => candidate.OrderItemId == record.OrderItemId)
                ?? throw new DomainRuleException("Production execution evidence is not present in its source command.");
            var recordLastUnit = checked(record.ProductionUnitNo + record.ProductionUnitQuantity - 1);
            var lineLastUnit = checked(line.ProductionUnitStartNo + line.Quantity - 1);
            if (record.ProductionUnitNo < line.ProductionUnitStartNo || recordLastUnit > lineLastUnit ||
                recordLastUnit > expectedQuantity)
                throw new DomainRuleException("Production execution evidence exceeds its dispatched unit range.");

            var attemptNo = command.DispatchAttemptNo ?? 0;
            var isRemake = string.Equals(payload.ExecutionIntent, "Remake", StringComparison.Ordinal);
            for (var unitNo = record.ProductionUnitNo; unitNo <= recordLastUnit; unitNo++)
            {
                var index = unitNo - 1;
                if (units[index] is not null && attempts[index] == attemptNo &&
                    units[index]!.SourceProductionJobId != record.SourceProductionJobId)
                    throw new DomainRuleException(
                        $"Production job unit range overlaps unit {unitNo} within dispatch attempt {attemptNo}.");
                if (units[index] is not null && attemptNo > attempts[index] && !isRemake)
                    throw new DomainRuleException(
                        "A later execution attempt cannot replace production-unit evidence unless it is an explicit remake.");
                if (units[index] is null || attemptNo >= attempts[index])
                {
                    units[index] = record;
                    attempts[index] = attemptNo;
                }
            }
        }

        return new ProductionUnitOutcomeSnapshot(1, expectedQuantity, units);
    }

    private static EdgeCommand ResolveCommand(
        ProductionExecutionRecord record,
        EdgeCommand currentCommand) =>
        record.SourceCommandId == currentCommand.Id
            ? currentCommand
            : record.SourceCommand ?? throw new DomainRuleException(
                "Production execution evidence is missing source-command provenance.");

    private int Count(ProductionExecutionStatus status) => Units.Count(record => record?.Status == status);
}
