using Application.Identity.Tokens.Claims;
using Application.Orders.Management.Commands;
using Application.Orders.Management.Requests;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.Common.Enums;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.SalesCatalog.Entities;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Orders.Persistence;
using Infrastructure.Payments.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IceBot.IntegrationTests.Orders;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class PaidOrderFulfillmentConcurrencyIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task OrderAndPaymentStores_SerializeOnTheSameOrderWorkflowIdentity()
    {
        var orderId = Guid.NewGuid();
        var firstAcquired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondAcquired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var orderDb = fixture.CreateDbContext();
        await using var paymentDb = fixture.CreateDbContext();
        var orderStore = new OrderStore(orderDb);
        var paymentStore = new PaymentStore(paymentDb);

        var first = orderStore.ExecuteInTransactionAsync(async ct =>
        {
            await orderStore.AcquireOrderWorkflowLockAsync(orderId, ct);
            firstAcquired.TrySetResult();
            await releaseFirst.Task.WaitAsync(ct);
            return true;
        });
        await firstAcquired.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var second = paymentStore.ExecuteInTransactionAsync(async ct =>
        {
            await paymentStore.AcquireOrderWorkflowLockAsync(orderId, ct);
            secondAcquired.TrySetResult();
            return true;
        });
        await Task.Delay(100);
        Assert.False(secondAcquired.Task.IsCompleted);

        releaseFirst.TrySetResult();
        await Task.WhenAll(first, second);
        Assert.True(secondAcquired.Task.IsCompletedSuccessfully);
    }

    [IntegrationFact]
    public async Task ConcurrentMixedItemCompletion_CompletesOrderAndPreservesBothHistories()
    {
        var seeded = await SeedMixedPaidOrderAsync();
        var user = new CurrentUserContext
        {
            AccountId = seeded.ActorAccountId,
            IsSystemAdmin = true
        };

        await using var manualDb = fixture.CreateDbContext();
        await using var packagedDb = fixture.CreateDbContext();
        var manualHandler = new RecordManualOrderItemFulfillmentEventCommandHandler(
            new OrderStore(manualDb), new NoOpRealtimeNotificationPublisher());
        var packagedHandler = new SetPackagedOrderItemFulfillmentCommandHandler(
            new OrderStore(packagedDb), new NoOpRealtimeNotificationPublisher());

        var manualEventId = Guid.NewGuid();
        var packagedEventId = Guid.NewGuid();
        var manualTask = manualHandler.HandleAsync(new RecordManualOrderItemFulfillmentEventCommand(
            seeded.OrderId,
            seeded.ManualItemId,
            user,
            new RecordManualOrderItemFulfillmentEventRequest
            {
                FulfillmentEventId = manualEventId,
                EventType = ManualOrderItemFulfillmentEventType.Completed
            }));
        var packagedTask = packagedHandler.HandleAsync(new SetPackagedOrderItemFulfillmentCommand(
            seeded.OrderId,
            seeded.PackagedItemId,
            packagedEventId,
            user,
            PackagedOrderItemFulfillmentAction.Fulfill));

        var results = await Task.WhenAll(manualTask, packagedTask);

        Assert.All(results, result => Assert.True(result.Succeeded, result.Message));
        await using var assertion = fixture.CreateDbContext();
        var order = await assertion.Orders.AsNoTracking()
            .Include(candidate => candidate.OrderItems)
            .SingleAsync(candidate => candidate.Id == seeded.OrderId);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.All(order.OrderItems, item => Assert.Equal(OrderItemStatus.Completed, item.Status));
        Assert.Equal(1, await assertion.OrderItemStatusHistories.CountAsync(history =>
            history.OrderItemId == seeded.ManualItemId && history.SourceEventId == manualEventId));
        Assert.Equal(1, await assertion.OrderItemStatusHistories.CountAsync(history =>
            history.OrderItemId == seeded.PackagedItemId && history.SourceEventId == packagedEventId));
    }

    [IntegrationFact]
    public async Task DefaultFulfillmentQueue_ExcludesFailedItems()
    {
        var seeded = await SeedMixedPaidOrderAsync(failPackagedItem: true);

        await using var db = fixture.CreateDbContext();
        var rows = await new OrderFulfillmentReadStore(db).ListQueueItemsAsync(
            kioskId: null,
            fulfillmentType: null,
            itemStatus: null,
            paidFrom: null,
            paidTo: null,
            includeTerminal: false,
            isSystemAdmin: true,
            allowedOrganizationIds: [],
            allowedStoreIds: [],
            allowedKioskIds: [],
            pageNumber: 1,
            pageSize: 500);

        Assert.DoesNotContain(rows, row => row.OrderItemId == seeded.PackagedItemId);
        Assert.Contains(rows, row => row.OrderItemId == seeded.ManualItemId);
    }

    [IntegrationFact]
    public async Task DefaultFulfillmentQueue_ExcludesRefundRequiredOrders()
    {
        var seeded = await SeedMixedPaidOrderAsync();
        await using (var mutation = fixture.CreateDbContext())
        {
            var order = await mutation.Orders.SingleAsync(candidate => candidate.Id == seeded.OrderId);
            order.MarkRefundRequired("Payment intervention required.");
            await mutation.SaveChangesAsync();
        }

        await using var db = fixture.CreateDbContext();
        var rows = await new OrderFulfillmentReadStore(db).ListQueueItemsAsync(
            null, null, null, null, null, false, true, [], [], [], 1, 500);

        Assert.DoesNotContain(rows, row => row.OrderId == seeded.OrderId);
    }

    private async Task<SeededOrder> SeedMixedPaidOrderAsync(bool failPackagedItem = false)
    {
        await using var db = fixture.CreateDbContext();
        var organization = new Organization
        {
            Code = $"MIXED-{Guid.NewGuid():N}", Name = "Mixed fulfillment organization", Status = EntityStatus.Active
        };
        var store = new Store
        {
            OrganizationId = organization.Id, Code = $"STORE-{Guid.NewGuid():N}",
            Name = "Mixed fulfillment store", Status = EntityStatus.Active
        };
        var kiosk = new Kiosk
        {
            OrganizationId = organization.Id, StoreId = store.Id,
            Code = $"KIOSK-{Guid.NewGuid():N}", Name = "Mixed fulfillment kiosk", Status = KioskStatus.Active
        };
        var product = new Product
        {
            OrganizationId = organization.Id, ScopeType = TenantScopeType.Organization,
            Code = $"PRODUCT-{Guid.NewGuid():N}", Name = "Mixed product",
            ProductType = "Mixed", BasePrice = 10_000, Currency = "VND"
        };
        var manualVariant = new ProductVariant
        {
            ProductId = product.Id, Code = "MANUAL", Name = "Manual",
            FulfillmentType = FulfillmentType.Manual, BasePrice = 10_000, Currency = "VND"
        };
        var packagedVariant = new ProductVariant
        {
            ProductId = product.Id, Code = "PACKAGED", Name = "Packaged",
            FulfillmentType = FulfillmentType.Packaged, BasePrice = 10_000, Currency = "VND"
        };
        var menu = new Menu
        {
            OrganizationId = organization.Id, StoreId = store.Id, KioskId = kiosk.Id,
            ScopeType = TenantScopeType.Kiosk, Code = $"MENU-{Guid.NewGuid():N}", Name = "Mixed menu"
        };
        var manualMenuItem = new MenuItem
        {
            MenuId = menu.Id, ProductId = product.Id, ProductVariantId = manualVariant.Id,
            Code = $"MANUAL-{Guid.NewGuid():N}", DisplayName = "Manual item", Price = 10_000, Currency = "VND"
        };
        var packagedMenuItem = new MenuItem
        {
            MenuId = menu.Id, ProductId = product.Id, ProductVariantId = packagedVariant.Id,
            Code = $"PACKAGED-{Guid.NewGuid():N}", DisplayName = "Packaged item", Price = 10_000, Currency = "VND"
        };
        product.ProductVariants.Add(manualVariant);
        product.ProductVariants.Add(packagedVariant);
        menu.MenuItems.Add(manualMenuItem);
        menu.MenuItems.Add(packagedMenuItem);
        var actor = new Account
        {
            UserName = $"fulfillment-{Guid.NewGuid():N}",
            Email = $"fulfillment-{Guid.NewGuid():N}@example.test",
            Status = AccountStatus.Active
        };
        db.AddRange(organization, store, kiosk, product, menu, actor);
        await db.SaveChangesAsync();

        var order = new Order
        {
            OrganizationId = organization.Id, StoreId = store.Id, KioskId = kiosk.Id,
            OrderNumber = $"ORDER-{Guid.NewGuid():N}"
        };
        order.SetCurrency("VND");
        var manualItem = order.AddItem(
            manualMenuItem.Id, product.Id, manualVariant.Id, null,
            manualMenuItem.Code, manualMenuItem.DisplayName, product.Code, product.Name,
            manualVariant.Code, manualVariant.Name, null, FulfillmentType.Manual, 1, 10_000);
        var packagedItem = order.AddItem(
            packagedMenuItem.Id, product.Id, packagedVariant.Id, null,
            packagedMenuItem.Code, packagedMenuItem.DisplayName, product.Code, product.Name,
            packagedVariant.Code, packagedVariant.Name, null, FulfillmentType.Packaged, 1, 10_000);
        var paidAt = DateTimeOffset.UtcNow;
        order.Place(paidAt);
        order.MarkPaid(order.TotalAmount, paidAt);
        manualItem.MarkAccepted();
        manualItem.MarkPreparing();
        if (failPackagedItem) packagedItem.FailPackaged("Test failure");
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return new SeededOrder(order.Id, manualItem.Id, packagedItem.Id, actor.Id);
    }

    private sealed record SeededOrder(
        Guid OrderId,
        Guid ManualItemId,
        Guid PackagedItemId,
        Guid ActorAccountId);
}
