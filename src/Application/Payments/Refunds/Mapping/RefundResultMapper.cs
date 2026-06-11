using Application.Payments.Refunds.Results;
using Domain.Payments.Entities;

namespace Application.Payments.Refunds.Mapping;

internal static class RefundResultMapper
{
    public static RefundResult ToResult(Refund refund)
    {
        var parsed = ParseReason(refund.Reason);
        return new RefundResult
        {
            Id = refund.Id,
            PaymentTransactionId = refund.PaymentTransactionId,
            RefundNumber = refund.RefundNumber,
            ProviderRefundId = refund.ProviderRefundId,
            Amount = refund.Amount,
            Currency = refund.Currency,
            Reason = parsed.Text,
            RefundMethod = parsed.Method,
            VoucherCode = parsed.Code,
            VoucherValue = parsed.Value,
            Note = parsed.Note,
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

    public static (string Method, string? Code, decimal? Value, string? Note, string Text) ParseReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return ("FullMoneyRefund", null, null, null, string.Empty);
        }

        if (reason.StartsWith("{") && reason.EndsWith("}"))
        {
            try
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<RefundReasonMetadata>(reason);
                if (data != null)
                {
                    return (data.Method ?? "FullMoneyRefund", data.Code, data.Value, data.Note, data.Text ?? string.Empty);
                }
            }
            catch
            {
                // fallback
            }
        }

        return ("FullMoneyRefund", null, null, null, reason);
    }

    private class RefundReasonMetadata
    {
        public string? Method { get; set; }
        public string? Code { get; set; }
        public decimal? Value { get; set; }
        public string? Note { get; set; }
        public string? Text { get; set; }
    }
}
