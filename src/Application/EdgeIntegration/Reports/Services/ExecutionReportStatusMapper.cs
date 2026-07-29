using Domain.Common;
using Domain.ProductionExecution.Enums;

namespace Application.EdgeIntegration.Reports.Services;

internal static class ExecutionReportStatusMapper
{
    public static bool Is(string status, string expected) =>
        string.Equals(status.Trim(), expected, StringComparison.OrdinalIgnoreCase);

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

    public static CustomerExecutionStatus ToCustomerStatus(ProductionExecutionStatus status, bool? physicalOutput) => status switch
    {
        ProductionExecutionStatus.Accepted or ProductionExecutionStatus.Running => CustomerExecutionStatus.Processing,
        ProductionExecutionStatus.Completed => CustomerExecutionStatus.Completed,
        ProductionExecutionStatus.Failed when physicalOutput == true => CustomerExecutionStatus.SupportRequired,
        ProductionExecutionStatus.Failed => CustomerExecutionStatus.Failed,
        ProductionExecutionStatus.RequiresManualIntervention => CustomerExecutionStatus.SupportRequired,
        _ => CustomerExecutionStatus.ExecutionUnconfirmed
    };
}
