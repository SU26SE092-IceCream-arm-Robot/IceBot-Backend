using Application.Orders.Admission;
using Domain.Catalog.Enums;
using Domain.Orders.Entities;
using Domain.Orders.Enums;

namespace IceBot.UnitTests.Orders;

public sealed class KioskCustomerSessionAdmissionTests
{
    [Fact]
    public void ActiveSessionPredicate_OnlyMatchesOrdersThatStillOccupyTheKiosk()
    {
        var kioskId = Guid.NewGuid();
        var observedAt = DateTimeOffset.UtcNow;
        var activePendingPayment = CreatePlacedOrder(kioskId, observedAt.AddMinutes(5));
        var expiredPendingPayment = CreatePlacedOrder(kioskId, observedAt.AddMinutes(-1));
        var completed = CreatePlacedOrder(kioskId, observedAt.AddMinutes(5));
        completed.MarkPaid(30_000, observedAt);
        completed.MarkAccepted();
        completed.MarkPreparing();
        completed.Complete(observedAt);

        var predicate = KioskCustomerSessionAdmission
            .BuildActiveSessionPredicate(kioskId, observedAt)
            .Compile();

        Assert.True(predicate(activePendingPayment));
        Assert.False(predicate(expiredPendingPayment));
        Assert.False(predicate(completed));
    }

    [Fact]
    public void ActiveSessionPredicate_ExcludesCurrentOrderButRetainsUnresolvedRefundIntervention()
    {
        var kioskId = Guid.NewGuid();
        var observedAt = DateTimeOffset.UtcNow;
        var currentOrder = CreatePlacedOrder(kioskId, observedAt.AddMinutes(5));
        var refundRequired = CreatePlacedOrder(kioskId, observedAt.AddMinutes(5));
        refundRequired.MarkPaid(30_000, observedAt);
        refundRequired.MarkRefundRequired("Manual resolution required.");

        var predicate = KioskCustomerSessionAdmission
            .BuildActiveSessionPredicate(kioskId, observedAt, currentOrder.Id)
            .Compile();

        Assert.False(predicate(currentOrder));
        Assert.True(predicate(refundRequired));
    }

    private static Order CreatePlacedOrder(Guid kioskId, DateTimeOffset paymentDeadlineAt)
    {
        var order = new Order { Id = Guid.NewGuid(), KioskId = kioskId, OrderNumber = Guid.NewGuid().ToString("N") };
        order.SetCurrency("VND");
        order.AddItem(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            "ITEM", "Item", "PRODUCT", "Product", "VARIANT", "Variant", null,
            FulfillmentType.MachineProduced, 1, 30_000);
        order.Place(paymentDeadlineAt.AddMinutes(-5), paymentDeadlineAt);
        return order;
    }
}
