namespace Application.Operations.MaintenanceTickets.Results;

public sealed class MaintenanceTicketResult
{
    public Guid Id { get; init; }
    public string TicketNumber { get; init; } = null!;
    public Guid OrganizationId { get; init; }
    public Guid StoreId { get; init; }
    public Guid KioskId { get; init; }
    public Guid? DeviceId { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? DeviceEventId { get; init; }
    public string IssueCode { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public string Priority { get; init; } = null!;
    public string Status { get; init; } = null!;
    public Guid? AssignedToAccountId { get; init; }
    public Guid? CreatedByAccountId { get; init; }
    public DateTimeOffset ReportedAt { get; init; }
    public DateTimeOffset? DueAt { get; init; }
    public DateTimeOffset? AssignedAt { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? ResolvedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public string? ResolutionNotes { get; init; }
    public string? CancelReason { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
