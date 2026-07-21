using Application.Tenants.Kiosks.Rules;
using Domain.Common;
using Domain.Devices.Connectivity;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;

namespace IceBot.UnitTests.Tenants;

public sealed class KioskOperationalStateTests
{
    [Fact]
    public void ChangeOperationalState_RecordsActorReasonAndTransition()
    {
        var kiosk = new Kiosk { Id = Guid.NewGuid() };
        var actorId = Guid.NewGuid();
        var changedAt = DateTimeOffset.UtcNow;

        var transition = kiosk.ChangeOperationalState(
            KioskOperationalState.Cleaning,
            "Scheduled cleaning",
            actorId,
            changedAt);

        Assert.NotNull(transition);
        Assert.Equal(KioskOperationalState.Operational, transition.FromState);
        Assert.Equal(KioskOperationalState.Cleaning, transition.ToState);
        Assert.Equal(KioskOperationalState.Cleaning, kiosk.OperationalState);
        Assert.Equal("Scheduled cleaning", kiosk.OperationalStateReason);
        Assert.Equal(actorId, kiosk.OperationalStateChangedByAccountId);
    }

    [Fact]
    public void ChangeOperationalState_RequiresReasonAndActor()
    {
        var kiosk = new Kiosk { Id = Guid.NewGuid() };

        Assert.Throws<DomainRuleException>(() => kiosk.ChangeOperationalState(
            KioskOperationalState.Maintenance,
            " ",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow));
        Assert.Throws<DomainRuleException>(() => kiosk.ChangeOperationalState(
            KioskOperationalState.Maintenance,
            "Repair",
            Guid.Empty,
            DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(KioskOperationalState.PausedByOperator)]
    [InlineData(KioskOperationalState.Maintenance)]
    [InlineData(KioskOperationalState.Cleaning)]
    [InlineData(KioskOperationalState.Restocking)]
    [InlineData(KioskOperationalState.EmergencyStopRequested)]
    [InlineData(KioskOperationalState.OutOfService)]
    public void NonOperationalState_BlocksSalesEvenWhenConnectivityIsOnline(
        KioskOperationalState state)
    {
        var kiosk = new Kiosk
        {
            Id = Guid.NewGuid(),
            Status = KioskStatus.Active,
            Store = new Store(),
            Organization = new Organization()
        };
        kiosk.ChangeOperationalState(state, "Operational work", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var connectivity = KioskConnectivityProjection.Create(kiosk.Id, DateTimeOffset.UtcNow);
        connectivity.Observe(
            KioskConnectivityStatus.Online,
            Guid.NewGuid(),
            1,
            DateTimeOffset.UtcNow);

        var error = KioskSalesAvailabilityRules.ValidateOnlineSalesAvailability(kiosk, connectivity);

        Assert.Contains("not accepting orders", error);
    }
}
