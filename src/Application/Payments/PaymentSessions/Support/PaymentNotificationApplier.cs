using Application.Payments.Providers;
using Domain.Orders.Enums;
using Domain.Payments.Entities;
using Domain.Payments.Enums;

namespace Application.Payments.PaymentSessions.Support;

internal static class PaymentNotificationApplier
{
    public static void ApplyNotification(PaymentTransaction paymentTransaction, ProviderPaymentNotification notification)
    {
        paymentTransaction.ProviderPaymentLinkId = notification.ProviderPaymentLinkId ?? paymentTransaction.ProviderPaymentLinkId;
        paymentTransaction.ProviderTransactionId = notification.ProviderTransactionId ?? paymentTransaction.ProviderTransactionId;
        paymentTransaction.ProviderStatus = notification.ProviderStatus;
        paymentTransaction.PaidAmount = notification.PaidAmount ?? paymentTransaction.PaidAmount;
        paymentTransaction.ProviderPaidAt = notification.ProviderPaidAt ?? paymentTransaction.ProviderPaidAt;
        paymentTransaction.RawResponseJson = notification.RawPayloadJson;

        if (notification.IsPaid)
        {
            if (paymentTransaction.Status == PaymentTransactionStatus.Refunded)
            {
                return;
            }

            var paidAt = notification.ProviderPaidAt ?? DateTimeOffset.UtcNow;
            var paidAmount = notification.PaidAmount ?? paymentTransaction.Amount;
            paymentTransaction.MarkPaid(notification.ProviderTransactionId, paidAt, notification.RawPayloadJson);
            paymentTransaction.Order.MarkPaid(paidAmount, paidAt);
            return;
        }

        if (notification.IsCancelled)
        {
            if (paymentTransaction.Status == PaymentTransactionStatus.Refunded)
            {
                return;
            }

            paymentTransaction.Cancel(DateTimeOffset.UtcNow);
            if (CanMarkOrderPaymentCancelled(paymentTransaction.Order.PaymentStatus))
            {
                paymentTransaction.Order.MarkPaymentCancelled();
            }
            return;
        }

        if (notification.IsExpired)
        {
            if (paymentTransaction.Status == PaymentTransactionStatus.Refunded)
            {
                return;
            }

            paymentTransaction.Cancel(DateTimeOffset.UtcNow);
            paymentTransaction.Status = PaymentTransactionStatus.Expired;
            if (CanMarkOrderPaymentCancelled(paymentTransaction.Order.PaymentStatus))
            {
                paymentTransaction.Order.MarkPaymentCancelled();
            }
        }
    }

    private static bool CanMarkOrderPaymentCancelled(PaymentStatus status) =>
        status is not (PaymentStatus.Paid or PaymentStatus.PartiallyRefunded or PaymentStatus.Refunded);
}
