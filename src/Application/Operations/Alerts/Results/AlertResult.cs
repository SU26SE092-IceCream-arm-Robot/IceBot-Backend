namespace Application.Operations.Alerts.Results;

public sealed class AlertResult
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid StoreId { get; init; }
    public Guid KioskId { get; init; }
    public Guid? DeviceId { get; init; }
    public required string AlertCode { get; init; }
    public required string Severity { get; init; }
    public required string Title { get; init; }
    public string? Message { get; init; }
    public required string Status { get; init; }
    public string? SourceType { get; init; }
    public Guid? SourceId { get; init; }
    public DateTimeOffset RaisedAt { get; init; }
    public Guid? AcknowledgedByAccountId { get; init; }
    public DateTimeOffset? AcknowledgedAt { get; init; }
    public DateTimeOffset? ResolvedAt { get; init; }
    public string? ResolutionNotes { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
