using Domain.SalesCatalog.Entities;
using Domain.SalesCatalog.Enums;

namespace IceBot.UnitTests.SalesCatalog;

public sealed class KioskMenuItemAvailabilityTests
{
    [Fact]
    public void Change_AppendsAuditableTransition_AndIncrementsRevision()
    {
        var availability = new KioskMenuItemAvailability
        {
            OrganizationId = Guid.NewGuid(),
            StoreId = Guid.NewGuid(),
            KioskId = Guid.NewGuid(),
            MenuId = Guid.NewGuid(),
            MenuItemId = Guid.NewGuid(),
            State = MenuItemOperationalAvailabilityState.Available,
            Revision = 0,
            ReasonCode = MenuItemOperationalAvailabilityReasonCode.ManualPause,
            Reason = "Initial state",
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedByAccountId = Guid.NewGuid()
        };
        var actorId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;

        availability.Change(
            MenuItemOperationalAvailabilityState.Paused,
            MenuItemOperationalAvailabilityReasonCode.OutOfStock,
            "Vanilla base is unavailable.",
            actorId,
            "Staff",
            "request-1",
            occurredAt);

        Assert.Equal(MenuItemOperationalAvailabilityState.Paused, availability.State);
        Assert.Equal(1, availability.Revision);
        var transition = Assert.Single(availability.Transitions);
        Assert.Equal(MenuItemOperationalAvailabilityState.Available, transition.FromState);
        Assert.Equal(MenuItemOperationalAvailabilityState.Paused, transition.ToState);
        Assert.Equal(availability.Revision, transition.AvailabilityRevision);
        Assert.Equal(actorId, transition.ActorAccountId);
        Assert.Equal("request-1", transition.RequestId);
    }
}
