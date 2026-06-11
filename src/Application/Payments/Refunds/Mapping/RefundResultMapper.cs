using Application.Payments.Refunds.Results;
using Domain.Payments.Entities;
using System;

namespace Application.Payments.Refunds.Mapping;

internal static class RefundResultMapper
{
    public static RefundResult ToResult(Refund refund)
    {
        return new RefundResult
        {
            Id = refund.Id,
            PaymentTransactionId = refund.PaymentTransactionId,
            RefundNumber = refund.RefundNumber,
            ProviderRefundId = refund.ProviderRefundId,
            Amount = refund.Amount,
            Currency = refund.Currency,
            Reason = refund.Reason,
            Status = refund.Status,
            RequestedAt = refund.RequestedAt,
            ProcessedAt = refund.ProcessedAt,
            RejectedAt = refund.RejectedAt,
            LastErrorCode = refund.LastErrorCode,
            LastErrorMessage = refund.LastErrorMessage,
            OrderId = refund.PaymentTransaction.OrderId,
            OrderNumber = refund.PaymentTransaction.Order.OrderNumber
        };
    }
}
