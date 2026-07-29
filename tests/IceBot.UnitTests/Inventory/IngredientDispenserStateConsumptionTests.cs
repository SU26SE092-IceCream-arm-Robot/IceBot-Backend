using Domain.Common;
using Domain.Inventory.Entities;

namespace IceBot.UnitTests.Inventory;

public sealed class IngredientDispenserStateConsumptionTests
{
    private static readonly DateTimeOffset OccurredAt =
        new(2026, 7, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ConsumeWithoutReportedBalance_DecrementsKnownEstimate()
    {
        var state = State(100);

        var movement = state.ConsumeWithEvidence(10, OccurredAt, null, "Order", Guid.NewGuid());

        Assert.Equal(90, state.EstimatedQuantity);
        Assert.Equal(100, movement.BalanceBefore);
        Assert.Equal(90, movement.BalanceAfter);
        Assert.Equal(-10, movement.Quantity);
    }

    [Fact]
    public void ConsumeWithMatchingReportedBalance_UsesComputedBalance()
    {
        var state = State(100);

        var movement = state.ConsumeWithEvidence(10, OccurredAt, 90);

        Assert.Equal(90, state.EstimatedQuantity);
        Assert.Equal(90, movement.BalanceAfter);
    }

    [Fact]
    public void ConsumeWithMismatchedReportedBalance_DoesNotMutateState()
    {
        var state = State(100);

        var exception = Assert.Throws<DomainRuleException>(
            () => state.ConsumeWithEvidence(10, OccurredAt, 95));

        Assert.Equal(
            "Reported stock balance does not match the dispenser estimate after consumption.",
            exception.Message);
        Assert.Equal(100, state.EstimatedQuantity);
    }

    [Fact]
    public void ConsumeWithUnknownEstimate_AcceptsReportedPostBalance()
    {
        var state = State(null);

        var movement = state.ConsumeWithEvidence(10, OccurredAt, 40);

        Assert.Equal(40, state.EstimatedQuantity);
        Assert.Null(movement.BalanceBefore);
        Assert.Equal(40, movement.BalanceAfter);
    }

    [Fact]
    public void ConsumeWithUnknownEstimateAndNoReportedBalance_RemainsUnknown()
    {
        var state = State(null);

        var movement = state.ConsumeWithEvidence(10, OccurredAt, null);

        Assert.Null(state.EstimatedQuantity);
        Assert.Null(movement.BalanceBefore);
        Assert.Null(movement.BalanceAfter);
    }

    private static IngredientDispenserState State(decimal? estimate) => new()
    {
        DeviceId = Guid.NewGuid(),
        IngredientId = Guid.NewGuid(),
        EstimatedQuantity = estimate,
        CapacityQuantity = 100,
        Unit = "gram",
        IsActive = true
    };
}
