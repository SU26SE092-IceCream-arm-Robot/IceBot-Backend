using Domain.Common;
using Domain.Devices.Catalog;
using Domain.Tenants.Enums;

namespace Domain.Tenants.Entities;

public partial class Kiosk : BusinessEntity, IOrganizationScoped
{
    public Guid OrganizationId { get; set; }

    Guid? IOrganizationScoped.OrganizationId
    {
        get => OrganizationId;
        set => OrganizationId = value ?? throw new InvalidOperationException("Kiosk.OrganizationId is required.");
    }

    public Guid StoreId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string KioskType { get; set; } = "RoboticVending";

    public KioskStatus Status { get; set; } = KioskStatus.Provisioning;

    public KioskOperationalState OperationalState { get; private set; } = KioskOperationalState.Operational;

    public string? OperationalStateReason { get; private set; }

    public DateTimeOffset? OperationalStateChangedAt { get; private set; }

    public Guid? OperationalStateChangedByAccountId { get; private set; }

    public string? SerialNumber { get; set; }

    public string TimeZone { get; set; } = "Asia/Bangkok";

    public string? Address { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public DateTimeOffset? InstalledAt { get; set; }

    public DateTimeOffset? LastOnlineAt { get; set; }

    public long ConfigurationVersion { get; set; }

    public int SettingsSchemaVersion { get; set; } = 1;

    public string? SettingsJson { get; set; }

    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();

    public virtual Organization Organization { get; set; } = null!;

    public virtual Store Store { get; set; } = null!;

    public KioskOperationalStateTransition? ChangeOperationalState(
        KioskOperationalState newState,
        string reason,
        Guid changedByAccountId,
        DateTimeOffset changedAt,
        Guid? sourceMaintenanceTicketId = null)
    {
        if (!Enum.IsDefined(newState))
        {
            throw new DomainRuleException("Invalid kiosk operational state.");
        }

        if (changedByAccountId == Guid.Empty)
        {
            throw new DomainRuleException("The operational-state actor is required.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleException("An operational-state reason is required.");
        }

        if (OperationalState == newState)
        {
            return null;
        }

        var transition = new KioskOperationalStateTransition
        {
            Id = Guid.NewGuid(),
            KioskId = Id,
            FromState = OperationalState,
            ToState = newState,
            Reason = reason.Trim(),
            ChangedAt = changedAt,
            ChangedByAccountId = changedByAccountId,
            SourceMaintenanceTicketId = sourceMaintenanceTicketId,
            CreatedAt = changedAt,
            CreatedByAccountId = changedByAccountId
        };

        OperationalState = newState;
        OperationalStateReason = transition.Reason;
        OperationalStateChangedAt = changedAt;
        OperationalStateChangedByAccountId = changedByAccountId;
        UpdatedAt = changedAt;
        UpdatedByAccountId = changedByAccountId;
        return transition;
    }
}
