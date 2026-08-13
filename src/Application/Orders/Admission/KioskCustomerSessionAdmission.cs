using System.Linq.Expressions;
using Domain.Orders.Entities;
using Domain.Orders.Enums;

namespace Application.Orders.Admission;

/// <summary>
/// Customer-attended kiosk admission is derived from Order lifecycle. It is
/// independent from the physical KioskOperationalState lifecycle.
/// </summary>
public static class KioskCustomerSessionAdmission
{
    public static Expression<Func<Order, bool>> BuildActiveSessionPredicate(
        Guid kioskId,
        DateTimeOffset observedAt,
        Guid? excludingOrderId = null) => order =>
        order.KioskId == kioskId &&
        (!excludingOrderId.HasValue || order.Id != excludingOrderId.Value) &&
        (
            (order.Status == OrderStatus.PendingPayment &&
             order.PaymentDeadlineAt != default && order.PaymentDeadlineAt > observedAt) ||
            order.Status == OrderStatus.Paid ||
            order.Status == OrderStatus.ReadyForFulfillment ||
            order.Status == OrderStatus.Accepted ||
            order.Status == OrderStatus.Preparing ||
            order.Status == OrderStatus.Ready ||
            order.Status == OrderStatus.FulfillmentIssue ||
            order.Status == OrderStatus.ExecutionRejected ||
            (order.Status == OrderStatus.RefundRequired &&
             order.OrderItems.Any(item => item.Status != OrderItemStatus.Completed &&
                                          item.Status != OrderItemStatus.Cancelled))
        );

    public static string OccupiedMessage =>
        "This kiosk is serving another customer. Please complete or resolve the active order before starting a new checkout.";
}
