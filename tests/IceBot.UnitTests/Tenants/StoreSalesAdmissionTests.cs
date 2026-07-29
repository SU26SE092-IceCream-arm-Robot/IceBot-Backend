using Application.Tenants.Stores;
using Domain.Tenants.Entities;

namespace IceBot.UnitTests.Tenants;

public sealed class StoreSalesAdmissionTests
{
    [Fact]
    public void ManualPause_BlocksNewSalesAdmission()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store();

        store.PauseSales(now, Guid.NewGuid(), "Early maintenance", now.AddHours(1));

        Assert.Equal(
            "Store is temporarily not accepting new orders.",
            StoreSalesAvailabilityRules.ValidateSalesAdmission(store, now.AddMinutes(1)));
    }

    [Fact]
    public void TimedPause_AutomaticallyStopsBlockingAfterResumeTime()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new Store();
        store.PauseSales(now, Guid.NewGuid(), "Short operational pause", now.AddMinutes(10));

        Assert.Null(StoreSalesAvailabilityRules.ValidateSalesAdmission(store, now.AddMinutes(10)));
    }

    [Fact]
    public void ManualResume_PreservesPauseEvidenceAndAllowsAdmission()
    {
        var now = DateTimeOffset.UtcNow;
        var actorId = Guid.NewGuid();
        var store = new Store();
        store.PauseSales(now, actorId, "Early maintenance", null);

        store.ResumeSales(now.AddMinutes(5), actorId);

        Assert.Null(StoreSalesAvailabilityRules.ValidateSalesAdmission(store, now.AddMinutes(6)));
        Assert.Equal("Early maintenance", store.SalesPauseReason);
        Assert.Equal(actorId, store.SalesPausedByAccountId);
        Assert.Equal(actorId, store.SalesResumedByAccountId);
    }
}
