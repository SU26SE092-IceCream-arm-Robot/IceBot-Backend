using Domain.Common;
using Domain.Common.Enums;
using Domain.Devices.Entities;
using Domain.Identity.Entities;
using Domain.Operations.Enums;
using Domain.Tenants.Entities;

namespace Domain.Operations.Entities;

public partial class Alert : SyncAggregateEntity
{
    public Guid KioskId { get; set; }

    public Guid? DeviceId { get; set; }

    public Guid? AcknowledgedByAccountId { get; set; }

    public string AlertCode { get; set; } = null!;

    public SeverityLevel Severity { get; set; } = SeverityLevel.Warning;

    public string Title { get; set; } = null!;

    public string? Message { get; set; }

    public AlertStatus Status { get; set; } = AlertStatus.Open;

    public string? SourceType { get; set; }

    public Guid? SourceId { get; set; }

    public DateTimeOffset RaisedAt { get; set; }

    public DateTimeOffset? AcknowledgedAt { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    public string? ResolutionNotes { get; set; }

    public virtual Account? AcknowledgedByAccount { get; set; }

    public virtual Device? Device { get; set; }

    public virtual Kiosk Kiosk { get; set; } = null!;

    public void Acknowledge(Guid acknowledgedByAccountId, DateTimeOffset acknowledgedAt)
    {
        if (Status == AlertStatus.Resolved)
        {
            throw new DomainRuleException("Cannot acknowledge a resolved alert.");
        }

        AcknowledgedByAccountId = acknowledgedByAccountId;
        AcknowledgedAt = acknowledgedAt;
        Status = AlertStatus.Acknowledged;
    }

    public void Resolve(DateTimeOffset resolvedAt, string? resolutionNotes = null)
    {
        if (Status == AlertStatus.Resolved)
        {
            return;
        }

        ResolvedAt = resolvedAt;
        ResolutionNotes = resolutionNotes;
        Status = AlertStatus.Resolved;
    }

    public void Suppress(DateTimeOffset suppressedAt, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleException("Suppress reason is required.");
        }

        ResolvedAt = suppressedAt;
        ResolutionNotes = reason.Trim();
        Status = AlertStatus.Suppressed;
    }
}
