using Domain.Catalog.Enums;
using Domain.Common;
using Domain.Orders.Entities;
using Domain.Orders.Enums;

namespace IceBot.UnitTests.Orders;

public sealed class OrderItemFulfillmentTests
{
    [Fact]
    public void FulfillPackaged_CompletesPendingItemAndIsIdempotent()
    {
        var item = CreateItem(FulfillmentType.Packaged);

        Assert.True(item.FulfillPackaged());
        Assert.Equal(OrderItemStatus.Completed, item.Status);
        Assert.False(item.FulfillPackaged());
    }

    [Fact]
    public void PackagedItem_CannotEnterGenericPreparingFlow()
    {
        var item = CreateItem(FulfillmentType.Packaged);

        var exception = Assert.Throws<DomainRuleException>(item.MarkPreparing);

        Assert.Equal("Packaged order items must use the packaged fulfillment transition.", exception.Message);
        Assert.Equal(OrderItemStatus.Pending, item.Status);
    }

    [Fact]
    public void FailPackaged_FailsPendingItemAndIsIdempotent()
    {
        var item = CreateItem(FulfillmentType.Packaged);

        Assert.True(item.FailPackaged("Item is out of stock."));
        Assert.Equal(OrderItemStatus.Failed, item.Status);
        Assert.False(item.FailPackaged("Repeated report."));
    }

    [Fact]
    public void ManualItem_CannotUsePackagedFulfillment()
    {
        var item = CreateItem(FulfillmentType.Manual);

        Assert.Throws<DomainRuleException>(() => item.FulfillPackaged());
        Assert.Equal(OrderItemStatus.Pending, item.Status);
    }

    [Fact]
    public void ManualItem_MustBeAcceptedBeforePreparing()
    {
        var item = CreateItem(FulfillmentType.Manual);

        var exception = Assert.Throws<DomainRuleException>(item.MarkPreparing);

        Assert.Equal("Only an accepted order item can be prepared.", exception.Message);
        Assert.Equal(OrderItemStatus.Pending, item.Status);
    }

    [Fact]
    public void MachineProducedItem_UsesExecutionLifecycle()
    {
        var item = CreateItem(FulfillmentType.MachineProduced);

        item.MarkAccepted();
        item.MarkPreparing();
        item.MarkCompleted();

        Assert.Equal(OrderItemStatus.Completed, item.Status);
    }

    private static OrderItem CreateItem(FulfillmentType fulfillmentType) => OrderItem.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        null,
        "MENU-ITEM",
        "Menu item",
        "PRODUCT",
        "Product",
        "VARIANT",
        "Variant",
        null,
        fulfillmentType,
        1,
        10_000m);
}
