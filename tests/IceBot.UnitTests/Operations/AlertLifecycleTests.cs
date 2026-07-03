using Domain.Common;
using Domain.Common.Enums;
using Domain.Operations.Entities;
using Domain.Operations.Enums;

namespace IceBot.UnitTests.Operations;

public sealed class AlertLifecycleTests
{
    [Fact]
    public void RecordOccurrence_UpdatesCorrelationMetadataAndOnlyRaisesSeverity()
    {
        var firstOccurredAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        var alert = Alert.RaiseFromDeviceEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), " motor-overheat ",
            SeverityLevel.Error, "Motor overheat", "First", firstOccurredAt,
            Guid.NewGuid(), firstOccurredAt);

        var latestSourceId = Guid.NewGuid();
        var latestOccurredAt = firstOccurredAt.AddMinutes(1);
        alert.RecordOccurrence(
            latestSourceId, SeverityLevel.Critical, "Motor overheat", "Second",
            latestOccurredAt, latestOccurredAt);
        alert.RecordOccurrence(
            Guid.NewGuid(), SeverityLevel.Error, "Motor overheat", "Third",
            latestOccurredAt.AddSeconds(1), latestOccurredAt.AddSeconds(1));

        Assert.Equal("MOTOR-OVERHEAT", alert.CorrelationKey);
        Assert.Equal(3, alert.OccurrenceCount);
        Assert.Equal(latestOccurredAt.AddSeconds(1), alert.LastOccurredAt);
        Assert.Equal(SeverityLevel.Critical, alert.Severity);
        Assert.Equal("Third", alert.Message);
        Assert.Equal(3, alert.Version);
    }

    [Fact]
    public void Acknowledge_IsIdempotent_AndResolveCompletesLifecycle()
    {
        var accountId = Guid.NewGuid();
        var acknowledgedAt = DateTimeOffset.UtcNow;
        var alert = new Alert { Status = AlertStatus.Open };

        alert.Acknowledge(accountId, acknowledgedAt);
        alert.Acknowledge(accountId, acknowledgedAt.AddMinutes(1));
        alert.Resolve(acknowledgedAt.AddMinutes(2), "Motor inspected and reset.");

        Assert.Equal(AlertStatus.Resolved, alert.Status);
        Assert.Equal(accountId, alert.AcknowledgedByAccountId);
        Assert.Equal(acknowledgedAt, alert.AcknowledgedAt);
        Assert.Equal("Motor inspected and reset.", alert.ResolutionNotes);
    }

    [Fact]
    public void SuppressedAlert_IsTerminalForAcknowledgeAndResolve()
    {
        var alert = new Alert { Status = AlertStatus.Open };
        alert.Suppress(DateTimeOffset.UtcNow, "Known maintenance window.");

        Assert.Throws<DomainRuleException>(() => alert.Acknowledge(Guid.NewGuid(), DateTimeOffset.UtcNow));
        Assert.Throws<DomainRuleException>(() => alert.Resolve(DateTimeOffset.UtcNow, "Resolved"));
    }
}
