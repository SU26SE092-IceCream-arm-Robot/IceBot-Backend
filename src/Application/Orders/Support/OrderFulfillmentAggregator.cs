using Domain.Orders.Entities;
using Domain.Orders.Enums;

namespace Application.Orders.Support;

internal static class OrderFulfillmentAggregator
{
    public static void Apply(Order order, DateTimeOffset changedAt, string? failureReason = null)
    {
        if (order.OrderItems.Count == 0) return;

        if (order.OrderItems.All(item => item.Status == OrderItemStatus.Completed))
        {
            if (order.Status != OrderStatus.Completed) order.Complete(changedAt);
            return;
        }

        if (order.OrderItems.Any(item => item.Status == OrderItemStatus.Failed))
        {
            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                if (order.Status != OrderStatus.FulfillmentIssue)
                    order.MarkFulfillmentIssue(failureReason);
            }
            else if (order.Status != OrderStatus.Failed)
            {
                order.MarkFailed(failureReason);
            }
            return;
        }

        if (order.OrderItems.Any(item => item.Status == OrderItemStatus.Preparing))
        {
            if (order.Status == OrderStatus.ReadyForFulfillment) order.MarkAccepted();
            if (order.Status == OrderStatus.Accepted) order.MarkPreparing();
            return;
        }

        if (order.OrderItems.Any(item => item.Status == OrderItemStatus.Accepted) &&
            order.Status == OrderStatus.ReadyForFulfillment)
            order.MarkAccepted();
    }
}
