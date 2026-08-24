using Domain.Orders.Entities;
using Domain.Catalog.Enums;

namespace IceBot.UnitTests.Orders;

public sealed class OrderItemPreparationSnapshotTests
{
    [Fact]
    public void Create_PersistsPositivePreparationSnapshot()
    {
        var item = OrderItem.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            "MENU", "Menu", "PRODUCT", "Product", "VARIANT", "Variant", null,
            FulfillmentType.Packaged, 1, 30_000, preparationTimeSecondsSnapshot: 90);

        Assert.Equal(90, item.PreparationTimeSecondsSnapshot);
    }

    [Fact]
    public void Create_NormalizesZeroPreparationSnapshotToNoPromise()
    {
        var item = OrderItem.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            "MENU", "Menu", "PRODUCT", "Product", "VARIANT", "Variant", null,
            FulfillmentType.Packaged, 1, 30_000, preparationTimeSecondsSnapshot: 0);

        Assert.Null(item.PreparationTimeSecondsSnapshot);
    }
}
