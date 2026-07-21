using Application.Orders.Management.Automation;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.Common.Enums;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Orders.Entities;
using Domain.SalesCatalog.Entities;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Operations.Persistence;
using Infrastructure.Orders.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IceBot.IntegrationTests.Orders;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class FulfillmentReminderIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task ConcurrentProcessing_EnqueuesOneDeliveryAndDoesNotStarveNextItem()
    {
        var firstItemId = await SeedOverdueOrderAsync();

        await using var firstDb = fixture.CreateDbContext();
        await using var secondDb = fixture.CreateDbContext();
        var first = new FulfillmentReminderService(
            new FulfillmentReminderStore(firstDb), new NotificationDeliveryStore(firstDb));
        var second = new FulfillmentReminderService(
            new FulfillmentReminderStore(secondDb), new NotificationDeliveryStore(secondDb));
        var now = DateTimeOffset.UtcNow;

        await Task.WhenAll(first.ProcessAsync(firstItemId, now), second.ProcessAsync(firstItemId, now));

        await using var assertion = fixture.CreateDbContext();
        Assert.Equal(1, await assertion.NotificationDeliveries.CountAsync(x =>
            x.NotificationType == "fulfillment_overdue" && x.SubjectId == firstItemId));

        var nextItemId = await SeedOverdueOrderAsync();
        await using var nextDb = fixture.CreateDbContext();
        var pending = await new FulfillmentReminderStore(nextDb).ListOverdueItemIdsAsync(now, 100);
        Assert.Contains(nextItemId, pending);
        Assert.DoesNotContain(firstItemId, pending);
    }

    [IntegrationFact]
    public async Task OlderItemWithoutRecipient_DoesNotStarveEligibleItem()
    {
        var now = DateTimeOffset.UtcNow;
        var noRecipientItemId = await SeedOverdueOrderAsync(false, now.AddMinutes(-20));
        var eligibleItemId = await SeedOverdueOrderAsync(true, now.AddMinutes(-10));

        await using var db = fixture.CreateDbContext();
        var pending = await new FulfillmentReminderStore(db).ListOverdueItemIdsAsync(now, 1);

        Assert.Equal([eligibleItemId], pending);
        Assert.DoesNotContain(noRecipientItemId, pending);
    }

    [IntegrationFact]
    public async Task FailedPackagedItem_IsNotSelectedForReminder()
    {
        var itemId = await SeedOverdueOrderAsync(failItem: true);

        await using var db = fixture.CreateDbContext();
        var pending = await new FulfillmentReminderStore(db)
            .ListOverdueItemIdsAsync(DateTimeOffset.UtcNow, 20);

        Assert.DoesNotContain(itemId, pending);
    }

    private async Task<Guid> SeedOverdueOrderAsync(
        bool includeRecipient = true,
        DateTimeOffset? paidAt = null,
        bool failItem = false)
    {
        await using var db = fixture.CreateDbContext();
        var organization = new Organization
        {
            Code = $"FULFILL-{Guid.NewGuid():N}", Name = "Fulfillment organization", Status = EntityStatus.Active
        };
        var store = new Store
        {
            OrganizationId = organization.Id, Code = $"STORE-{Guid.NewGuid():N}",
            Name = "Fulfillment store", Status = EntityStatus.Active
        };
        var kiosk = new Kiosk
        {
            OrganizationId = organization.Id, StoreId = store.Id,
            Code = $"KIOSK-{Guid.NewGuid():N}", Name = "Fulfillment kiosk", Status = KioskStatus.Active
        };
        var product = new Product
        {
            OrganizationId = organization.Id, ScopeType = TenantScopeType.Organization,
            Code = $"PRODUCT-{Guid.NewGuid():N}", Name = "Packaged product",
            ProductType = "Packaged", BasePrice = 10_000, Currency = "VND"
        };
        var variant = new ProductVariant
        {
            ProductId = product.Id, Code = "DEFAULT", Name = "Default",
            FulfillmentType = FulfillmentType.Packaged, BasePrice = 10_000,
            Currency = "VND", PreparationTimeSeconds = 1
        };
        var menu = new Menu
        {
            OrganizationId = organization.Id, StoreId = store.Id, KioskId = kiosk.Id,
            ScopeType = TenantScopeType.Kiosk, Code = $"MENU-{Guid.NewGuid():N}", Name = "Menu"
        };
        var menuItem = new MenuItem
        {
            MenuId = menu.Id, ProductId = product.Id, ProductVariantId = variant.Id,
            Code = $"ITEM-{Guid.NewGuid():N}", DisplayName = "Ready item",
            Price = 10_000, Currency = "VND", PreparationTimeSeconds = 1
        };
        product.ProductVariants.Add(variant);
        menu.MenuItems.Add(menuItem);

        Account? account = null;
        Role? staffRole = null;
        if (includeRecipient)
        {
            account = new Account
            {
                UserName = $"staff-{Guid.NewGuid():N}", Email = $"staff-{Guid.NewGuid():N}@example.test",
                Status = AccountStatus.Active
            };
            account.NotificationDevices.Add(new AccountNotificationDevice
            {
                AccountId = account.Id, InstallationId = Guid.NewGuid(), Platform = "Android",
                PushToken = $"token-{Guid.NewGuid():N}", PushTokenHash = $"hash-{Guid.NewGuid():N}"
            });
            staffRole = await db.Roles.SingleOrDefaultAsync(x => x.Code == "Staff");
            if (staffRole is null)
            {
                staffRole = new Role { Code = "Staff", Name = "Staff", IsSystemRole = true };
                db.Roles.Add(staffRole);
                await db.SaveChangesAsync();
            }
        }
        db.AddRange(organization, store, kiosk, product, menu);
        if (account is not null) db.Add(account);
        await db.SaveChangesAsync();
        if (account is not null && staffRole is not null)
        {
            db.AccountRoles.Add(new AccountRole
            {
                AccountId = account.Id, RoleId = staffRole.Id, OrganizationId = organization.Id,
                StoreId = store.Id, KioskId = kiosk.Id, AssignedAt = DateTimeOffset.UtcNow
            });
        }

        var order = new Order
        {
            OrganizationId = organization.Id, StoreId = store.Id, KioskId = kiosk.Id,
            OrderNumber = $"ORDER-{Guid.NewGuid():N}"
        };
        order.SetCurrency("VND");
        var item = order.AddItem(menuItem.Id, product.Id, variant.Id, null,
            menuItem.Code, menuItem.DisplayName, product.Code, product.Name, variant.Code, variant.Name,
            null, FulfillmentType.Packaged, 1, 10_000);
        var paymentTimestamp = paidAt ?? DateTimeOffset.UtcNow.AddMinutes(-10);
        order.Place(paymentTimestamp, paymentTimestamp.AddMinutes(15));
        order.MarkPaid(order.TotalAmount, paymentTimestamp);
        if (failItem) item.FailPackaged("Test failure");
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return item.Id;
    }
}
