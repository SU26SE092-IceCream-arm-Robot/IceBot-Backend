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
            var paidAt = notification.ProviderPaidAt ?? DateTimeOffset.UtcNow;
            var paidAmount = notification.PaidAmount ?? paymentTransaction.Amount;
            paymentTransaction.MarkPaid(notification.ProviderTransactionId, paidAt, notification.RawPayloadJson);
            paymentTransaction.Order.MarkPaid(paidAmount, paidAt);
            return;
        }

        if (notification.IsCancelled)
        {
            paymentTransaction.Cancel(DateTimeOffset.UtcNow);
            if (paymentTransaction.Order.PaymentStatus != PaymentStatus.Paid)
            {
                paymentTransaction.Order.MarkPaymentCancelled();
            }
            return;
        }

        if (notification.IsExpired)
        {
            paymentTransaction.Cancel(DateTimeOffset.UtcNow);
            paymentTransaction.Status = PaymentTransactionStatus.Expired;
            if (paymentTransaction.Order.PaymentStatus != PaymentStatus.Paid)
            {
                paymentTransaction.Order.MarkPaymentCancelled();
            }
        }
    }
}
