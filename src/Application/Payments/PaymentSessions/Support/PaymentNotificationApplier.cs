using Application.Payments.Providers;
using Domain.Orders.Enums;
using Domain.Payments.Entities;
using Domain.Payments.Enums;

namespace Application.Payments.PaymentSessions.Support;

internal enum PaymentNotificationValidationFailure
{
    PaidAmountMissing,
    PaidAmountMismatch
}

internal static class PaymentNotificationApplier
{
    public static PaymentNotificationValidationFailure? ValidateNotification(
        PaymentTransaction paymentTransaction,
        ProviderPaymentNotification notification)
    {
        if (!notification.IsPaid)
        {
            return null;
        }

        if (!notification.PaidAmount.HasValue)
        {
            return PaymentNotificationValidationFailure.PaidAmountMissing;
        }

        return notification.PaidAmount.Value == paymentTransaction.Amount
            ? null
            : PaymentNotificationValidationFailure.PaidAmountMismatch;
    }

    public static string ToDiagnosticCode(PaymentNotificationValidationFailure failure) => failure switch
    {
        PaymentNotificationValidationFailure.PaidAmountMissing => "PAID_AMOUNT_MISSING",
        PaymentNotificationValidationFailure.PaidAmountMismatch => "PAID_AMOUNT_MISMATCH",
        _ => throw new ArgumentOutOfRangeException(nameof(failure), failure, "Unsupported payment notification validation failure.")
    };

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
            paymentTransaction.MarkPaid(notification.ProviderTransactionId, paidAt, notification.RawPayloadJson);
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

            paymentTransaction.MarkExpired(DateTimeOffset.UtcNow);
            if (CanMarkOrderPaymentCancelled(paymentTransaction.Order.PaymentStatus))
            {
                paymentTransaction.Order.MarkPaymentCancelled();
            }
        }
    }

    private static bool CanMarkOrderPaymentCancelled(PaymentStatus status) =>
        status is not (PaymentStatus.Paid or PaymentStatus.PartiallyRefunded or PaymentStatus.Refunded);
}
