using Application.Payments.PaymentSessions.Results;
using Domain.Payments.Entities;

namespace Application.Payments.PaymentSessions.Mapping;

internal static class PaymentSessionResultMapper
{
    public static PaymentSessionResult ToSessionResult(PaymentTransaction paymentTransaction)
    {
        return new PaymentSessionResult
        {
            PaymentTransactionId = paymentTransaction.Id,
            OrderId = paymentTransaction.OrderId,
            TransactionNumber = paymentTransaction.TransactionNumber,
            Provider = paymentTransaction.Provider,
            CheckoutUrl = paymentTransaction.CheckoutUrl,
            QrCodePayload = paymentTransaction.QrCodePayload,
            Amount = paymentTransaction.Amount,
            Currency = paymentTransaction.Currency,
            Status = paymentTransaction.Status,
            ExpiresAt = paymentTransaction.ExpiresAt
        };
    }
}
