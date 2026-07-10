using Application.Operations.OperationLogs.Results;
using Domain.Operations.Entities;

namespace Application.Operations.OperationLogs.Mapping;

public static class OperationLogResultMapper
{
    public static OperationLogResult ToResult(OperationLog log) => new()
    {
        Id = log.Id,
        KioskId = log.KioskId ?? Guid.Empty,
        DeviceId = log.DeviceId,
        OrderId = log.OrderId,
        Action = log.Action,
        Category = log.Category,
        Severity = log.Severity.ToString(),
        Message = log.Message,
        OccurredAt = log.OccurredAt
    };

    public static OperationLogDiagnosticsResult ToDiagnosticsResult(OperationLog log) => new()
    {
        Id = log.Id,
        PayloadJson = log.PayloadJson
    };
}
