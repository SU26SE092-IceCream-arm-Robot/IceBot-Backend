using Application.SalesCatalog.Abstractions;
using Application.SalesCatalog.ReadModels;
using Application.SalesCatalog.RuntimeMenus.Queries;
using Domain.Common.Enums;
using Domain.Devices.Connectivity;
using Domain.SalesCatalog.Entities;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using NSubstitute;

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
        var handler = new GetKioskRuntimeMenuQueryHandler(store);

        var first = await handler.HandleAsync(new GetKioskRuntimeMenuQuery(kiosk.Id));
        var second = await handler.HandleAsync(new GetKioskRuntimeMenuQuery(kiosk.Id));

        Assert.True(first.Succeeded, first.Message);
        Assert.True(second.Succeeded, second.Message);
        Assert.NotEqual(first.Data!.SnapshotId, second.Data!.SnapshotId);
        Assert.Equal(first.Data.Revision, second.Data.Revision);
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
