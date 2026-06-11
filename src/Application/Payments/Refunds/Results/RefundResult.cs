using Domain.Payments.Enums;
using System;

namespace Application.Payments.Refunds.Results;

public sealed class RefundResult
{
    public Guid Id { get; set; }
    public Guid PaymentTransactionId { get; set; }
    public string RefundNumber { get; set; } = null!;
    public string? ProviderRefundId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string Reason { get; set; } = null!;
    public RefundStatus Status { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
    public string? LastErrorCode { get; set; }
    public string? LastErrorMessage { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = null!;
}
