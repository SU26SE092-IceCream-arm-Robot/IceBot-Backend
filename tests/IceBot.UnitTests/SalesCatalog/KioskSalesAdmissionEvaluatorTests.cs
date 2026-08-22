using Application.Devices.Telemetry;
using Application.SalesCatalog.Admission;
using Application.SalesCatalog.Admission.Abstractions;
using Application.SalesCatalog.Admission.Services;
using Application.Tenants.Kiosks.Rules;
using Domain.Common.Enums;
using Domain.Devices.Connectivity;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace IceBot.UnitTests.SalesCatalog;

public sealed class KioskSalesAdmissionEvaluatorTests
{
    [Fact]
    public async Task CustomerSessionOccupied_LeavesCatalogVisible_ButBlocksOrderAndPayment()
    {
        var kiosk = ActiveKiosk();
        var connectivity = KioskConnectivityProjection.Create(kiosk.Id, DateTimeOffset.UtcNow);
        connectivity.Observe(KioskConnectivityStatus.Online, Guid.NewGuid(), 1, DateTimeOffset.UtcNow);
        var store = Substitute.For<IOperationalAdmissionReadStore>();
        store.GetKioskConnectivityAsync(kiosk.Id, Arg.Any<CancellationToken>()).Returns(connectivity);
        store.HasActiveCustomerSessionAsync(kiosk.Id, Arg.Any<DateTimeOffset>(), null, Arg.Any<CancellationToken>()).Returns(true);
        var evaluator = new KioskSalesAdmissionEvaluator(
            store,
            Options.Create(new KioskSalesAdmissionOptions { RequireConnectivity = true }),
            Options.Create(new EdgeTelemetryIngestionOptions { HeartbeatTimeoutSeconds = 90 }));

        var decision = await evaluator.EvaluateAsync(kiosk, new KioskSalesAdmissionRequest(DateTimeOffset.UtcNow));

        Assert.True(decision.CanExposeCatalog);
        Assert.False(decision.CanPlaceOrder);
        Assert.False(decision.CanOpenPayment);
        Assert.Contains(decision.Blockers, item => item.Code == SalesAdmissionBlockerCode.CustomerSessionOccupied);
        await store.Received(1).GetKioskConnectivityAsync(kiosk.Id, Arg.Any<CancellationToken>());
    }

    private static Kiosk ActiveKiosk()
    {
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Code = "ORG",
            Name = "Organization",
            Status = EntityStatus.Active
        };
        var store = new Store
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Code = "STORE",
            Name = "Store",
            Status = EntityStatus.Active,
            Organization = organization
        };
        return new Kiosk
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            StoreId = store.Id,
            Code = "KIOSK",
            Name = "Kiosk",
            Status = KioskStatus.Active,
            Organization = organization,
            Store = store
        };
    }
}
