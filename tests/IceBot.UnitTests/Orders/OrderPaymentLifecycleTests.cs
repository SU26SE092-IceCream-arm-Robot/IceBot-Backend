using Domain.Common;
using Domain.Orders.Entities;
using Domain.Orders.Enums;

namespace IceBot.UnitTests.Orders;

public sealed class OrderPaymentLifecycleTests
{
    [Fact]
    public void MarkPaymentCancelled_RejectsRefundedPayment()
    {
        var order = CreateRefundedOrder();

        var exception = Assert.Throws<DomainRuleException>(order.MarkPaymentCancelled);

        Assert.Equal("A paid or refunded order payment status cannot be cancelled.", exception.Message);
        Assert.Equal(PaymentStatus.Refunded, order.PaymentStatus);
    }

    [Fact]
    public void MarkPaid_AfterOrderCancellation_RequiresRefundReview()
    {
        var order = new Order();
        order.Cancel(DateTimeOffset.UtcNow, "Customer cancelled before payment completed.");
        order.MarkPaymentCancelled();

        order.MarkPaid(1, DateTimeOffset.UtcNow);

        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(OrderStatus.RefundRequired, order.Status);
    }

    private static Order CreateRefundedOrder()
    {
        var now = DateTimeOffset.UtcNow;
        var order = new Order();
        order.MarkPaid(1, now);
        order.MarkRefundRequired();
        order.MarkRefunded();
        order.MarkPaymentRefunded();
        return order;
    }
}
