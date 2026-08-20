using Domain.Common;
using Domain.Identity.Entities;
using Domain.Payments.Enums;

namespace Domain.Payments.Entities;

public partial class Refund : BusinessEntity
{
    public Guid PaymentTransactionId { get; set; }

    public Guid? RequestedByAccountId { get; set; }

    public string RefundNumber { get; set; } = null!;

    public string? IdempotencyKey { get; set; }

    public Guid? CorrelationId { get; set; }

    public string? ProviderRefundId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "VND";

    public RefundCompensationMethod CompensationMethod { get; set; } = RefundCompensationMethod.FullMoneyRefund;

    public string Reason { get; set; } = null!;

    public RefundStatus Status { get; set; } = RefundStatus.Requested;

    public DateTimeOffset RequestedAt { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public DateTimeOffset? RejectedAt { get; set; }

    public int RetryCount { get; set; }

    public int MaxRetries { get; set; } = 3;

    public DateTimeOffset? NextRetryAt { get; set; }

    public DateTimeOffset? LastAttemptAt { get; set; }

    public string? LastErrorCode { get; set; }

    public string? LastErrorMessage { get; set; }

    public virtual PaymentTransaction PaymentTransaction { get; set; } = null!;

    public virtual Account? RequestedByAccount { get; set; }

    public void MarkProcessing(DateTimeOffset lastAttemptAt)
    {
        if (Status is RefundStatus.Processed or RefundStatus.Cancelled)
        {
            throw new DomainRuleException("Cannot process a finalized refund.");
        }

        LastAttemptAt = lastAttemptAt;
        Status = RefundStatus.Processing;
    }

    public void MarkProcessed(string? providerRefundId, DateTimeOffset processedAt)
    {
        if (Amount <= 0)
        {
            throw new DomainRuleException("Refund amount must be greater than zero.");
        }

        if (Status == RefundStatus.Cancelled)
        {
            throw new DomainRuleException("Cannot process a cancelled refund.");
        }

        ProviderRefundId = providerRefundId ?? ProviderRefundId;
        ProcessedAt = processedAt;
        Status = RefundStatus.Processed;
        ClearRetryState();
    }

    public void Reject(DateTimeOffset rejectedAt, string reason)
    {
        if (Status == RefundStatus.Processed)
        {
            throw new DomainRuleException("Cannot reject a processed refund.");
        }

        RejectedAt = rejectedAt;
        LastErrorMessage = reason;
        Status = RefundStatus.Rejected;
    }

    public void MarkFailed(string errorCode, string errorMessage)
    {
        if (Status == RefundStatus.Processed)
        {
            throw new DomainRuleException("Cannot fail a processed refund.");
        }

        LastErrorCode = errorCode;
        LastErrorMessage = errorMessage;
        Status = RefundStatus.Failed;
    }

    public void ScheduleRetry(string errorCode, string errorMessage, DateTimeOffset nextRetryAt)
    {
        if (!CanRetry)
        {
            throw new DomainRuleException("Refund retry limit has been reached.");
        }

        RetryCount++;
        LastErrorCode = errorCode;
        LastErrorMessage = errorMessage;
        NextRetryAt = nextRetryAt;
        Status = RefundStatus.Processing;
    }

    public void Cancel()
    {
        if (Status is RefundStatus.Processed or RefundStatus.Processing)
        {
            throw new DomainRuleException("Cannot cancel a processed or processing refund.");
        }

        Status = RefundStatus.Cancelled;
    }

    public bool CanRetry => Status is RefundStatus.Requested or RefundStatus.Processing or RefundStatus.Failed
        && RetryCount < MaxRetries;

    private void ClearRetryState()
    {
        NextRetryAt = null;
        LastErrorCode = null;
        LastErrorMessage = null;
    }
}
