using Application.SalesCatalog.Abstractions;
using Application.Devices.Telemetry;
using Application.SalesCatalog.Admission;
using Application.SalesCatalog.Admission.Abstractions;
using Application.SalesCatalog.Admission.Services;
using Application.SalesCatalog.ReadModels;
using Application.SalesCatalog.RuntimeMenus.Queries;
using Application.SalesCatalog.RuntimeMenus.Abstractions;
using Application.SalesCatalog.RuntimeMenus.Results;
using Application.SalesCatalog.RuntimeMenus.Services;
using Application.Tenants.Kiosks.Rules;
using Domain.Common.Enums;
using Domain.Devices.Connectivity;
using Domain.SalesCatalog.Entities;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using NSubstitute;
using Microsoft.Extensions.Options;

namespace IceBot.UnitTests.SalesCatalog;

public sealed class RuntimeMenuRevisionTests
{
    [Fact]
    public async Task SameSellableContent_ProducesStableRevisionAcrossSnapshots()
    {
        var kiosk = ActiveKiosk();
        var connectivity = KioskConnectivityProjection.Create(kiosk.Id, DateTimeOffset.UtcNow);
        connectivity.Observe(
            KioskConnectivityStatus.Online,
            Guid.NewGuid(),
            1,
            DateTimeOffset.UtcNow);
        var store = Substitute.For<IMenuStore>();
        store.GetKioskByIdAsync(kiosk.Id, Arg.Any<CancellationToken>()).Returns(kiosk);
        store.GetKioskConnectivityAsync(kiosk.Id, Arg.Any<CancellationToken>()).Returns(connectivity);
        store.ListActiveMenusForKioskAsync(
                kiosk.OrganizationId, kiosk.StoreId, kiosk.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<Menu>());
        store.ListMenuItemProductOptionsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<MenuItemProductOptionReadModel>());
        store.ListMenuItemOptionGroupsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<MenuItemOptionGroupReadModel>());
        var admissionStore = AdmissionStore(connectivity);
        var handler = new GetKioskRuntimeMenuQueryHandler(
            store,
            new RuntimeMenuProjectionBuilder(store),
            new PassthroughRuntimeMenuCache(),
            KioskAdmission(admissionStore),
            ItemAdmission(admissionStore));

        var first = await handler.HandleAsync(new GetKioskRuntimeMenuQuery(kiosk.Id));
        var second = await handler.HandleAsync(new GetKioskRuntimeMenuQuery(kiosk.Id));

        Assert.True(first.Succeeded, first.Message);
        Assert.True(second.Succeeded, second.Message);
        Assert.NotEqual(first.Data!.SnapshotId, second.Data!.SnapshotId);
        Assert.Equal(first.Data.Revision, second.Data.Revision);
    }

    [Fact]
    public async Task OfflineKiosk_ExposesCatalogStateButBlocksOrderAdmission()
    {
        var kiosk = ActiveKiosk();
        var connectivity = KioskConnectivityProjection.Create(kiosk.Id, DateTimeOffset.UtcNow);
        connectivity.Observe(
            KioskConnectivityStatus.Unreachable,
            Guid.NewGuid(),
            1,
            DateTimeOffset.UtcNow);
        var store = Substitute.For<IMenuStore>();
        store.GetKioskByIdAsync(kiosk.Id, Arg.Any<CancellationToken>()).Returns(kiosk);
        store.GetKioskConnectivityAsync(kiosk.Id, Arg.Any<CancellationToken>()).Returns(connectivity);
        store.ListActiveMenusForKioskAsync(
                kiosk.OrganizationId, kiosk.StoreId, kiosk.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<Menu>());
        store.ListMenuItemProductOptionsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<MenuItemProductOptionReadModel>());
        store.ListMenuItemOptionGroupsAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<MenuItemOptionGroupReadModel>());
        var admissionStore = AdmissionStore(connectivity);
        var cache = new RecordingRuntimeMenuCache();
        var handler = new GetKioskRuntimeMenuQueryHandler(
            store,
            new RuntimeMenuProjectionBuilder(store),
            cache,
            KioskAdmission(admissionStore),
            ItemAdmission(admissionStore));

        var result = await handler.HandleAsync(new GetKioskRuntimeMenuQuery(kiosk.Id));

        Assert.True(result.Succeeded, result.Message);
        Assert.Empty(result.Data!.Items);
        Assert.False(result.Data.Admission!.CanPlaceOrder);
        Assert.False(result.Data.Admission.CanOpenPayment);
        Assert.Equal(1, cache.ReadCount);
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

    private static IOperationalAdmissionReadStore AdmissionStore(KioskConnectivityProjection connectivity)
    {
        var store = Substitute.For<IOperationalAdmissionReadStore>();
        store.GetKioskConnectivityAsync(connectivity.KioskId, Arg.Any<CancellationToken>()).Returns(connectivity);
        store.HasActiveCustomerSessionAsync(
                connectivity.KioskId,
                Arg.Any<DateTimeOffset>(),
                null,
                Arg.Any<CancellationToken>())
            .Returns(false);
        return store;
    }

    private static KioskSalesAdmissionEvaluator KioskAdmission(IOperationalAdmissionReadStore store) =>
        new(
            store,
            Options.Create(new KioskSalesAdmissionOptions()),
            Options.Create(new EdgeTelemetryIngestionOptions()));

    private static MenuItemOperationalAdmissionEvaluator ItemAdmission(IOperationalAdmissionReadStore store) =>
        new(
            store,
            null!,
            Options.Create(new EdgeTelemetryIngestionOptions()));

    private sealed class PassthroughRuntimeMenuCache : IRuntimeMenuProjectionCache
    {
        public async Task<RuntimeMenuCachedProjection> GetOrCreateAsync(
            Guid kioskId,
            Func<CancellationToken, Task<RuntimeMenuProjection>> factory,
            CancellationToken cancellationToken = default)
        {
            var projection = await factory(cancellationToken);
            return new RuntimeMenuCachedProjection(
                projection.Revision,
                projection.Items,
                DateTimeOffset.UtcNow.AddSeconds(15));
        }
    }

    private sealed class RecordingRuntimeMenuCache : IRuntimeMenuProjectionCache
    {
        public int ReadCount { get; private set; }

        public async Task<RuntimeMenuCachedProjection> GetOrCreateAsync(
            Guid kioskId,
            Func<CancellationToken, Task<RuntimeMenuProjection>> factory,
            CancellationToken cancellationToken = default)
        {
            ReadCount++;
            var projection = await factory(cancellationToken);
            return new RuntimeMenuCachedProjection(
                projection.Revision,
                projection.Items,
                DateTimeOffset.UtcNow.AddSeconds(15));
        }
    }
}
