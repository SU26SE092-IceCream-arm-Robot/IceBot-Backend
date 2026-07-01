using Domain.Common;
using Domain.Common.Enums;
using Domain.Devices.Entities;
using Domain.Identity.Entities;
using Domain.Operations.Enums;
using Domain.Tenants.Entities;

namespace Domain.Operations.Entities;

public partial class Alert : SyncAggregateEntity
{
    public static Alert RaiseFromDeviceEvent(
        Guid kioskId,
        Guid deviceId,
        Guid deviceEventId,
        string alertCode,
        SeverityLevel severity,
        string title,
        string? message,
        DateTimeOffset raisedAt,
        Guid originNodeId,
        DateTimeOffset syncedAt)
    {
        if (kioskId == Guid.Empty || deviceId == Guid.Empty || deviceEventId == Guid.Empty ||
            originNodeId == Guid.Empty || string.IsNullOrWhiteSpace(alertCode) ||
            string.IsNullOrWhiteSpace(title))
        {
            throw new DomainRuleException("Device-event alert identity, code, and title are required.");
        }

        return new Alert
        {
            KioskId = kioskId,
            DeviceId = deviceId,
            AlertCode = alertCode.Trim(),
            Severity = severity,
            Title = title.Trim(),
            Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
            Status = AlertStatus.Open,
            SourceType = "DeviceEvent",
            SourceId = deviceEventId,
            RaisedAt = raisedAt,
            OriginNodeId = originNodeId,
            Version = 1,
            SyncedAt = syncedAt
        };
    }

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
        if (Status is AlertStatus.Resolved or AlertStatus.Suppressed)
        {
            throw new DomainRuleException("Cannot acknowledge a terminal alert.");
        }

        if (Status == AlertStatus.Acknowledged)
        {
            return;
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

        if (Status == AlertStatus.Suppressed)
        {
            throw new DomainRuleException("Cannot resolve a suppressed alert.");
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
