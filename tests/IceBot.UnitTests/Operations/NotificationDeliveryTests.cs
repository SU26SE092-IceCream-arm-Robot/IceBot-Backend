using Domain.Common;
using Domain.Operations.Entities;
using Domain.Operations.Enums;

namespace IceBot.UnitTests.Operations;

public sealed class NotificationDeliveryTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ProcessingFailure_RetriesUntilMaximumThenBecomesPermanent()
    {
        var delivery = Create(maxAttempts: 2);

        delivery.MarkProcessing(Now, TimeSpan.FromMinutes(1));
        delivery.MarkFailed("TEMPORARY", "provider unavailable", Now.AddMinutes(1));
        Assert.Equal(NotificationDeliveryStatus.Failed, delivery.Status);

        delivery.MarkProcessing(Now.AddMinutes(1), TimeSpan.FromMinutes(1));
        delivery.MarkFailed("TEMPORARY", "provider unavailable", Now.AddMinutes(2));

        Assert.Equal(NotificationDeliveryStatus.PermanentFailure, delivery.Status);
        Assert.Equal(2, delivery.AttemptCount);
    }

    [Fact]
    public void StaleProcessingDelivery_CanBeReclaimed()
    {
        var delivery = Create();
        delivery.MarkProcessing(Now, TimeSpan.FromMinutes(1));

        Assert.False(delivery.CanBeClaimed(Now.AddSeconds(59), TimeSpan.FromMinutes(1)));
        Assert.True(delivery.CanBeClaimed(Now.AddMinutes(1), TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void DeliveredNotification_CannotBeClaimedAgain()
    {
        var delivery = Create();
        delivery.MarkProcessing(Now, TimeSpan.FromMinutes(1));
        delivery.MarkDelivered(Now.AddSeconds(1));

        Assert.Equal(NotificationDeliveryStatus.Delivered, delivery.Status);
        Assert.False(delivery.CanBeClaimed(Now.AddDays(1), TimeSpan.FromMinutes(1)));
        Assert.Throws<DomainRuleException>(() =>
            delivery.MarkProcessing(Now.AddDays(1), TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void FinalAttemptThatBecomesStale_IsRecoverableForTerminalizationNotAnotherSend()
    {
        var delivery = Create(maxAttempts: 1);
        delivery.MarkProcessing(Now, TimeSpan.FromMinutes(1));

        Assert.True(delivery.IsStaleProcessing(Now.AddMinutes(1), TimeSpan.FromMinutes(1)));
        Assert.False(delivery.CanBeClaimed(Now.AddMinutes(1), TimeSpan.FromMinutes(1)));

        delivery.MarkPermanentFailure("PROCESSING_TIMEOUT_EXHAUSTED", "outcome unknown");
        Assert.Equal(NotificationDeliveryStatus.PermanentFailure, delivery.Status);
    }

    [Fact]
    public void PermanentFailure_CanBeRequeuedWithFreshRetryBudget()
    {
        var delivery = Create(maxAttempts: 1);
        delivery.MarkProcessing(Now, TimeSpan.FromMinutes(1));
        delivery.MarkFailed("FAILED", "provider unavailable", Now.AddMinutes(1));

        delivery.Requeue(Guid.NewGuid(), Now.AddMinutes(2));

        Assert.Equal(NotificationDeliveryStatus.Pending, delivery.Status);
        Assert.Equal(0, delivery.AttemptCount);
        Assert.Null(delivery.LastErrorCode);
        Assert.Equal(Now.AddMinutes(2), delivery.NextAttemptAt);
    }

    [Fact]
    public void NonTerminalDelivery_CannotBeRequeued()
    {
        var delivery = Create();
        Assert.Throws<DomainRuleException>(() => delivery.Requeue(Guid.NewGuid(), Now));
    }

    private static NotificationDelivery Create(int maxAttempts = 5) =>
        NotificationDelivery.CreatePush(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            $"test:{Guid.NewGuid():N}",
            "test",
            Guid.NewGuid(),
            "title",
            "body",
            "{}",
            Now,
            maxAttempts);
}
