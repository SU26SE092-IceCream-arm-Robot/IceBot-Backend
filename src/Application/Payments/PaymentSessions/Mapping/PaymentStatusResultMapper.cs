using Application.Payments.PaymentSessions.Results;
using Domain.Payments.Entities;

namespace Application.Payments.PaymentSessions.Mapping;

internal static class PaymentStatusResultMapper
{
    public static PaymentStatusResult ToStatusResult(PaymentTransaction paymentTransaction)
    {
        var customerStatusInfo = Application.Shared.Utils.OrderStatusProjector.ProjectFromTransaction(paymentTransaction.Status, paymentTransaction.Order);

        return new PaymentStatusResult
        {
            PaymentTransactionId = paymentTransaction.Id,
            OrderId = paymentTransaction.OrderId,
            Provider = paymentTransaction.Provider,
            PaymentTransactionStatus = paymentTransaction.Status,
            OrderPaymentStatus = paymentTransaction.Order.PaymentStatus,
            OrderStatus = paymentTransaction.Order.Status,
            Amount = paymentTransaction.Amount,
            PaidAmount = paymentTransaction.PaidAmount,
            Currency = paymentTransaction.Currency,
            PaidAt = paymentTransaction.PaidAt,
            ExpiresAt = paymentTransaction.ExpiresAt,
            CustomerStatus = customerStatusInfo.CustomerStatus,
            CustomerStatusMessage = customerStatusInfo.CustomerStatusMessage,
            CanRetryPayment = customerStatusInfo.CanRetryPayment,
            RequiresStaffSupport = customerStatusInfo.RequiresStaffSupport
        };
    }
}
