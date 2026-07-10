using Domain.Devices.Telemetry;
using Domain.Common;
using Domain.Common.Enums;
using Domain.Devices.Catalog;
using Domain.Identity.Entities;
using Domain.Operations.Enums;
using Domain.Tenants.Entities;

namespace Domain.Operations.Entities;

public partial class Alert : SyncAggregateEntity
{
    public static string NormalizeCorrelationKey(string alertCode)
    {
        if (string.IsNullOrWhiteSpace(alertCode))
        {
            throw new DomainRuleException("Alert code is required.");
        }

        return alertCode.Trim().ToUpperInvariant();
    }

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
            CorrelationKey = NormalizeCorrelationKey(alertCode),
            Severity = severity,
            Title = title.Trim(),
            Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
            Status = AlertStatus.Open,
            SourceType = "DeviceEvent",
            SourceId = deviceEventId,
            RaisedAt = raisedAt,
            LastOccurredAt = raisedAt,
            OccurrenceCount = 1,
            OriginNodeId = originNodeId,
            Version = 1,
            SyncedAt = syncedAt
        };
    }

    public Guid KioskId { get; set; }

    public Guid? DeviceId { get; set; }

    public Guid? AcknowledgedByAccountId { get; set; }

    public string AlertCode { get; set; } = null!;

    public string CorrelationKey { get; private set; } = null!;

    public SeverityLevel Severity { get; set; } = SeverityLevel.Warning;

    public string Title { get; set; } = null!;

    public string? Message { get; set; }

    public AlertStatus Status { get; set; } = AlertStatus.Open;

    public string? SourceType { get; set; }

    public Guid? SourceId { get; set; }

    public DateTimeOffset RaisedAt { get; set; }

    public DateTimeOffset LastOccurredAt { get; private set; }

    public int OccurrenceCount { get; private set; } = 1;

    public DateTimeOffset? AcknowledgedAt { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    public string? ResolutionNotes { get; set; }

    public virtual Account? AcknowledgedByAccount { get; set; }

    public virtual Device? Device { get; set; }

    public virtual Kiosk Kiosk { get; set; } = null!;

    public void RecordOccurrence(
        Guid sourceId,
        SeverityLevel severity,
        string title,
        string? message,
        DateTimeOffset occurredAt,
        DateTimeOffset syncedAt)
    {
        if (Status is AlertStatus.Resolved or AlertStatus.Suppressed)
        {
            throw new DomainRuleException("Cannot correlate an occurrence into a terminal alert.");
        }

        if (sourceId == Guid.Empty || string.IsNullOrWhiteSpace(title))
        {
            throw new DomainRuleException("Alert occurrence source and title are required.");
        }

        OccurrenceCount = checked(OccurrenceCount + 1);
        LastOccurredAt = occurredAt > LastOccurredAt ? occurredAt : LastOccurredAt;
        SourceId = sourceId;
        Severity = severity > Severity ? severity : Severity;
        Title = title.Trim();
        Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim();
        SyncedAt = syncedAt;
        Version++;
    }

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
