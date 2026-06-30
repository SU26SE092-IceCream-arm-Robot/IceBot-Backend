using Domain.Common;
using Domain.Operations.Entities;
using Domain.Operations.Enums;

namespace IceBot.UnitTests.Operations;

public sealed class AlertLifecycleTests
{
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
