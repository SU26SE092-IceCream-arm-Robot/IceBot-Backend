namespace Application.Operations.OperationLogs.Results;

public sealed class OperationLogResult
{
    public Guid Id { get; init; }
    public Guid KioskId { get; init; }
    public Guid? DeviceId { get; init; }
    public Guid? OrderId { get; init; }
    public string Action { get; init; } = null!;
    public string Category { get; init; } = null!;
    public string Severity { get; init; } = null!;
    public string? Message { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}

public sealed class OperationLogDiagnosticsResult
{
    public Guid Id { get; init; }
    public string? PayloadJson { get; init; }
}
