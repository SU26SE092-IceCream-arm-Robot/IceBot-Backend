using Domain.Common;
using Domain.Devices.Entities;
using Domain.Identity.Entities;
using Domain.Operations.Enums;
using Domain.Tenants.Entities;

namespace Domain.Operations.Entities;

public partial class MaintenanceTicket : RobotRuntimeAggregateEntity
{
    public Guid KioskId { get; set; }

    public Guid? DeviceId { get; set; }

    public Guid? AssignedToAccountId { get; set; }

    public string TicketNumber { get; set; } = null!;

    public string IssueCode { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;

    public MaintenanceTicketStatus Status { get; set; } = MaintenanceTicketStatus.Open;

    public DateTimeOffset ReportedAt { get; set; }

    public DateTimeOffset? DueAt { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public string? ResolutionNotes { get; set; }

    public virtual Account? AssignedToAccount { get; set; }

    public virtual Account? CreatedByAccount { get; set; }

    public virtual Device? Device { get; set; }

    public virtual Kiosk Kiosk { get; set; } = null!;

    public void Assign(Guid accountId)
    {
        if (Status is MaintenanceTicketStatus.Resolved or MaintenanceTicketStatus.Closed or MaintenanceTicketStatus.Cancelled)
        {
            throw new DomainRuleException("Cannot assign a finalized maintenance ticket.");
        }

        AssignedToAccountId = accountId;
        Status = MaintenanceTicketStatus.Assigned;
    }

    public void StartWork()
    {
        if (Status is MaintenanceTicketStatus.Resolved or MaintenanceTicketStatus.Closed or MaintenanceTicketStatus.Cancelled)
        {
            throw new DomainRuleException("Cannot start work on a finalized maintenance ticket.");
        }

        Status = MaintenanceTicketStatus.InProgress;
    }

    public void Resolve(DateTimeOffset resolvedAt, string resolutionNotes)
    {
        if (string.IsNullOrWhiteSpace(resolutionNotes))
        {
            throw new DomainRuleException("Resolution notes are required.");
        }

        if (Status == MaintenanceTicketStatus.Cancelled)
        {
            throw new DomainRuleException("Cannot resolve a cancelled maintenance ticket.");
        }

        ResolvedAt = resolvedAt;
        ResolutionNotes = resolutionNotes.Trim();
        Status = MaintenanceTicketStatus.Resolved;
    }

    public void Close(DateTimeOffset closedAt)
    {
        if (Status != MaintenanceTicketStatus.Resolved)
        {
            throw new DomainRuleException("Only resolved maintenance tickets can be closed.");
        }

        ClosedAt = closedAt;
        Status = MaintenanceTicketStatus.Closed;
    }

    public void Cancel(DateTimeOffset cancelledAt, string reason)
    {
        if (Status == MaintenanceTicketStatus.Closed)
        {
            throw new DomainRuleException("Cannot cancel a closed maintenance ticket.");
        }

        ClosedAt = cancelledAt;
        ResolutionNotes = reason;
        Status = MaintenanceTicketStatus.Cancelled;
    }
}
