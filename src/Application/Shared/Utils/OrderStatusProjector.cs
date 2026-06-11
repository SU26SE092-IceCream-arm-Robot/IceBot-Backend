using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.Payments.Enums;

namespace Application.Shared.Utils;

public static class OrderStatusProjector
{
    public static (string CustomerStatus, string CustomerStatusMessage, bool CanRetryPayment, bool RequiresStaffSupport) ProjectFromOrder(Order order, PaymentTransactionStatus? latestTransactionStatus = null)
    {
        if (order.Status == OrderStatus.Draft)
        {
            return ("Draft", "Order is not placed yet.", false, false);
        }

        if (order.Status == OrderStatus.PendingPayment)
        {
            if (order.PaymentStatus == PaymentStatus.Cancelled)
            {
                return ("PaymentCancelled", "Payment was cancelled. You can try paying again.", true, false);
            }
            if (order.PaymentStatus == PaymentStatus.Failed)
            {
                return ("PaymentFailed", "Payment failed. You can try paying again.", true, false);
            }
            if (latestTransactionStatus == PaymentTransactionStatus.Expired)
            {
                return ("PaymentExpired", "Payment session expired. Please retry.", true, false);
            }

            return ("WaitingForPayment", "Waiting for payment. Please scan the QR code.", true, false);
        }

        if (order.Status is OrderStatus.Paid or OrderStatus.ReadyForExecution or OrderStatus.Accepted or OrderStatus.Preparing)
        {
            return ("Preparing", "Payment successful. Preparing your order.", false, false);
        }

        if (order.Status == OrderStatus.Ready)
        {
            return ("Ready", "Your order is ready. Please pick it up!", false, false);
        }

        if (order.Status == OrderStatus.Completed)
        {
            return ("Completed", "Order completed. Thank you!", false, false);
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                return ("RefundRequired", "Order cancelled after payment. Please contact staff for a refund.", false, true);
            }
            return ("Cancelled", "Order cancelled.", false, false);
        }

        if (order.Status is OrderStatus.Failed or OrderStatus.ExecutionRejected or OrderStatus.RefundRequired)
        {
            return ("RefundRequired", "Order execution failed. Please contact staff for support.", false, true);
        }

        if (order.Status == OrderStatus.Refunded)
        {
            return ("Refunded", "Order has been refunded.", false, false);
        }

        if (order.Status == OrderStatus.Compensated)
        {
            return ("Compensated", "Order has been compensated (Voucher issued).", false, false);
        }

        return ("Unknown", "Unknown order state.", false, false);
    }

    public static (string CustomerStatus, string CustomerStatusMessage, bool CanRetryPayment, bool RequiresStaffSupport) ProjectFromTransaction(PaymentTransactionStatus transactionStatus, Order order)
    {
        if (order.PaymentStatus == PaymentStatus.Paid ||
            order.Status is OrderStatus.Paid or OrderStatus.ReadyForExecution or OrderStatus.Accepted or OrderStatus.Preparing or OrderStatus.Ready or OrderStatus.Completed)
        {
            return ProjectFromOrder(order);
        }

        if (transactionStatus == PaymentTransactionStatus.Paid)
        {
            return ("Preparing", "Payment successful. Preparing your order.", false, false);
        }

        if (transactionStatus == PaymentTransactionStatus.Cancelled)
        {
            return ("PaymentCancelled", "Payment was cancelled. You can try paying again.", order.Status == OrderStatus.PendingPayment, false);
        }

        if (transactionStatus == PaymentTransactionStatus.Expired)
        {
            return ("PaymentExpired", "Payment session expired. Please retry.", order.Status == OrderStatus.PendingPayment, false);
        }

        if (transactionStatus == PaymentTransactionStatus.Failed)
        {
            return ("PaymentFailed", "Payment failed. You can try paying again.", order.Status == OrderStatus.PendingPayment, false);
        }

        if (transactionStatus == PaymentTransactionStatus.Refunded)
        {
            return ("Refunded", "Order refunded.", false, false);
        }

        return ("WaitingForPayment", "Waiting for payment. Please scan the QR code.", true, false);
    }
}
