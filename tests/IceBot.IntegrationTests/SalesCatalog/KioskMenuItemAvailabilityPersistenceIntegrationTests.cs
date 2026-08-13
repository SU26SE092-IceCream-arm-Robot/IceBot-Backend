using Application.SalesCatalog.Availability;
using Domain.Catalog.Entities;
using Domain.Common.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using Domain.SalesCatalog.Entities;
using Domain.SalesCatalog.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.SalesCatalog.Persistence;

namespace IceBot.IntegrationTests.SalesCatalog;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class KioskMenuItemAvailabilityPersistenceIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task RequestReplay_ReturnsTheOriginalAppliedTransition_NotTheLaterCurrentState()
    {
        await using var db = fixture.CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var organization = new Organization { Id = Guid.CreateVersion7(), Code = $"ORG-{Guid.NewGuid():N}", Name = "Availability test", Status = EntityStatus.Active, CreatedAt = now };
        var store = new Store { Id = Guid.CreateVersion7(), OrganizationId = organization.Id, Code = $"STORE-{Guid.NewGuid():N}", Name = "Availability store", Status = EntityStatus.Active, CreatedAt = now };
        var kiosk = new Kiosk { Id = Guid.CreateVersion7(), OrganizationId = organization.Id, StoreId = store.Id, Code = $"KIOSK-{Guid.NewGuid():N}", Name = "Availability kiosk", CreatedAt = now };
        var product = new Product { Id = Guid.CreateVersion7(), OrganizationId = organization.Id, ScopeType = TenantScopeType.Organization, Code = $"PRODUCT-{Guid.NewGuid():N}", Name = "Availability product", Currency = "VND", IsAvailable = true, CreatedAt = now };
        var variant = new ProductVariant { Id = Guid.CreateVersion7(), ProductId = product.Id, Code = $"VARIANT-{Guid.NewGuid():N}", Name = "Availability variant", Currency = "VND", IsAvailable = true, CreatedAt = now };
        var menu = new Menu { Id = Guid.CreateVersion7(), OrganizationId = organization.Id, ScopeType = TenantScopeType.Organization, Code = $"MENU-{Guid.NewGuid():N}", Name = "Availability menu", Currency = "VND", Status = MenuStatus.Active, CreatedAt = now };
        var item = new MenuItem { Id = Guid.CreateVersion7(), MenuId = menu.Id, ProductId = product.Id, ProductVariantId = variant.Id, Code = $"ITEM-{Guid.NewGuid():N}", DisplayName = "Availability item", Currency = "VND", Status = MenuItemStatus.Active, CreatedAt = now };
        var availability = new KioskMenuItemAvailability { OrganizationId = organization.Id, StoreId = store.Id, KioskId = kiosk.Id, MenuId = menu.Id, MenuItemId = item.Id, State = MenuItemOperationalAvailabilityState.Available, ReasonCode = MenuItemOperationalAvailabilityReasonCode.ManualPause, Reason = "Initial", ChangedAt = now, ChangedByAccountId = Guid.NewGuid(), CreatedAt = now };
        availability.Change(MenuItemOperationalAvailabilityState.Paused, MenuItemOperationalAvailabilityReasonCode.OutOfStock, "Out of vanilla", Guid.NewGuid(), "Staff", "request-pause", now.AddMinutes(1));
        availability.Change(MenuItemOperationalAvailabilityState.Available, MenuItemOperationalAvailabilityReasonCode.ManualPause, "Restocked", Guid.NewGuid(), "Staff", "request-resume", now.AddMinutes(2));

        db.AddRange(organization, store, kiosk, product, variant, menu, item, availability);
        await db.SaveChangesAsync();

        var replay = await new MenuStore(db).GetKioskMenuItemAvailabilityByRequestIdAsync(kiosk.Id, item.Id, "request-pause");

        Assert.NotNull(replay);
        Assert.Equal(MenuItemOperationalAvailabilityState.Paused, replay.RequestedState);
        Assert.Equal(1, replay.AppliedRevision);
        Assert.Equal("Out of vanilla", replay.RequestedReason);
    }
}
