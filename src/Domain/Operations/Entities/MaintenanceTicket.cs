using Domain.Common;
using Domain.Devices.Entities;
using Domain.Identity.Entities;
using Domain.Operations.Enums;
using Domain.Orders.Entities;
using Domain.Tenants.Entities;

namespace Domain.Operations.Entities;

public partial class MaintenanceTicket : SyncAggregateEntity, IKioskScoped
{
    public Guid OrganizationId { get; set; }

    public Guid StoreId { get; set; }

    public Guid KioskId { get; set; }

    public Guid? DeviceId { get; set; }

    public Guid? OrderId { get; set; }

    public Guid? DeviceEventId { get; set; }

    public Guid? AssignedToAccountId { get; set; }

    public string TicketNumber { get; set; } = null!;

    public string IssueCode { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;

    public MaintenanceTicketStatus Status { get; set; } = MaintenanceTicketStatus.Open;

    public DateTimeOffset ReportedAt { get; set; }

    public DateTimeOffset? DueAt { get; set; }

    public DateTimeOffset? AssignedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public string? ResolutionNotes { get; set; }

    public string? CancelReason { get; set; }

    public virtual Organization Organization { get; set; } = null!;

    public virtual Store Store { get; set; } = null!;

    public virtual Kiosk Kiosk { get; set; } = null!;

    public virtual Account? AssignedToAccount { get; set; }

    public virtual Account? CreatedByAccount { get; set; }

    public virtual Device? Device { get; set; }

    public virtual Order? Order { get; set; }

    public virtual DeviceEvent? DeviceEvent { get; set; }

    Guid? IOrganizationScoped.OrganizationId
    {
        get => OrganizationId;
        set => OrganizationId = value ?? Guid.Empty;
    }

    Guid? IStoreScoped.StoreId
    {
        get => StoreId;
        set => StoreId = value ?? Guid.Empty;
    }

    Guid? IKioskScoped.KioskId
    {
        get => KioskId;
        set => KioskId = value ?? Guid.Empty;
    }

    public void Assign(Guid accountId)
    {
        if (Status != MaintenanceTicketStatus.Open)
        {
            throw new DomainRuleException("Only open maintenance tickets can be assigned.");
        }

        AssignedToAccountId = accountId;
        AssignedAt = DateTimeOffset.UtcNow;
        Status = MaintenanceTicketStatus.Assigned;
    }

    public void StartWork()
    {
        if (Status is not (MaintenanceTicketStatus.Open or MaintenanceTicketStatus.Assigned))
        {
            throw new DomainRuleException("Only open or assigned maintenance tickets can be started.");
        }

        StartedAt = DateTimeOffset.UtcNow;
        Status = MaintenanceTicketStatus.InProgress;
    }

    public void Resolve(DateTimeOffset resolvedAt, string resolutionNotes)
    {
        if (string.IsNullOrWhiteSpace(resolutionNotes))
        {
            throw new DomainRuleException("Resolution notes are required.");
        }

        if (Status != MaintenanceTicketStatus.InProgress)
        {
            throw new DomainRuleException("Only in-progress maintenance tickets can be resolved.");
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
        if (Status is not (MaintenanceTicketStatus.Open or MaintenanceTicketStatus.Assigned or MaintenanceTicketStatus.InProgress))
        {
            throw new DomainRuleException("Only open, assigned, or in-progress maintenance tickets can be cancelled.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleException("Cancel reason is required.");
        }

        CancelledAt = cancelledAt;
        CancelReason = reason.Trim();
        Status = MaintenanceTicketStatus.Cancelled;
    }
}
