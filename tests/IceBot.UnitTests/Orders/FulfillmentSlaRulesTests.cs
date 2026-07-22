using Application.Orders.Management.Results;
using Application.Orders.Management.Rules;
using Domain.Orders.Enums;

namespace IceBot.UnitTests.Orders;

public sealed class FulfillmentSlaRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MissingPreparationTime_IsNotConfigured()
    {
        var result = FulfillmentSlaRules.Project(Now, null, OrderItemStatus.Pending, Now);

        Assert.Null(result.ExpectedReadyAt);
        Assert.Equal(FulfillmentSlaStatus.NotConfigured, result.Status);
    }

    [Theory]
    [InlineData(-121, FulfillmentSlaStatus.OnTrack)]
    [InlineData(-120, FulfillmentSlaStatus.DueSoon)]
    [InlineData(0, FulfillmentSlaStatus.Overdue)]
    public void ActiveItem_ProjectsDeadlineRelativeState(
        int observedOffsetSeconds,
        FulfillmentSlaStatus expectedStatus)
    {
        var paidAt = Now;
        var expectedReadyAt = paidAt.AddMinutes(5);
        var observedAt = expectedReadyAt.AddSeconds(observedOffsetSeconds);

        var result = FulfillmentSlaRules.Project(
            paidAt, 300, OrderItemStatus.Pending, observedAt);

        Assert.Equal(expectedReadyAt, result.ExpectedReadyAt);
        Assert.Equal(expectedStatus, result.Status);
    }

    [Fact]
    public void CompletedItem_RemainsTerminalAfterDeadline()
    {
        var result = FulfillmentSlaRules.Project(
            Now, 60, OrderItemStatus.Completed, Now.AddHours(1));

        Assert.Equal(FulfillmentSlaStatus.Terminal, result.Status);
    }
}
