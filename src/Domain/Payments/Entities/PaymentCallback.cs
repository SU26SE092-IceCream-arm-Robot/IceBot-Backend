using Domain.Common;
using Domain.Payments.Enums;

namespace Domain.Payments.Entities;

public partial class PaymentCallback : AppendOnlyEntity
{
    public Guid PaymentTransactionId { get; set; }

    public string Provider { get; set; } = null!;

    public string EventType { get; set; } = null!;

    public string? ProviderEventId { get; set; }

    public string PayloadJson { get; set; } = null!;

    public string? Signature { get; set; }

    public PaymentCallbackProcessingStatus ProcessingStatus { get; set; } = PaymentCallbackProcessingStatus.Received;

    public int ProcessingAttempts { get; set; }

    public int MaxProcessingAttempts { get; set; } = 5;

    public DateTimeOffset ReceivedAt { get; set; }

    public DateTimeOffset? LastAttemptAt { get; set; }

    public DateTimeOffset? NextRetryAt { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public string? LastError { get; set; }

    public virtual PaymentTransaction PaymentTransaction { get; set; } = null!;

    public void MarkProcessing(DateTimeOffset attemptedAt)
    {
        if (ProcessingStatus == PaymentCallbackProcessingStatus.Processed)
        {
            throw new DomainRuleException("Cannot process an already processed payment callback.");
        }

        ProcessingStatus = PaymentCallbackProcessingStatus.Processing;
        LastAttemptAt = attemptedAt;
        ProcessingAttempts++;
    }

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        ProcessedAt = processedAt;
        NextRetryAt = null;
        LastError = null;
        ProcessingStatus = PaymentCallbackProcessingStatus.Processed;
    }

    public void MarkFailed(string error, DateTimeOffset? nextRetryAt)
    {
        LastError = error;
        NextRetryAt = nextRetryAt;
        ProcessingStatus = CanRetry ? PaymentCallbackProcessingStatus.Failed : PaymentCallbackProcessingStatus.Ignored;
    }

    public void MarkIgnored(string reason, DateTimeOffset ignoredAt)
    {
        LastError = reason;
        LastAttemptAt = ignoredAt;
        NextRetryAt = null;
        ProcessingStatus = PaymentCallbackProcessingStatus.Ignored;
    }

    public bool CanRetry => ProcessingAttempts < MaxProcessingAttempts;
}
