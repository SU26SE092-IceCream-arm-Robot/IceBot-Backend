using Domain.Common;
using Domain.Orders.Entities;
using Domain.Payments.Enums;

namespace Domain.Payments.Entities;

public partial class PaymentTransaction : BusinessEntity
{
    public Guid OrderId { get; set; }

    public long PaymentMethodId { get; set; }

    public string TransactionNumber { get; set; } = null!;

    public string? IdempotencyKey { get; set; }

    public Guid? CorrelationId { get; set; }

    public string Provider { get; set; } = null!;

    public string? ProviderTransactionId { get; set; }

    public string? PaymentIntentId { get; set; }

    public string? ProviderOrderCode { get; set; }

    public string? ProviderPaymentLinkId { get; set; }

    public string? CheckoutUrl { get; set; }

    public string? QrCodePayload { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public decimal Amount { get; set; }

    public decimal? PaidAmount { get; set; }

    public string Currency { get; set; } = "VND";

    public string? ProviderStatus { get; set; }

    public DateTimeOffset? ProviderPaidAt { get; set; }

    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Pending;

    public PaymentSettlementDisposition SettlementDisposition { get; private set; }

    public DateTimeOffset RequestedAt { get; set; }

    public DateTimeOffset? AuthorizedAt { get; set; }

    public DateTimeOffset? PaidAt { get; set; }

    public DateTimeOffset? FailedAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public int RetryCount { get; set; }

    public int MaxRetries { get; set; } = 3;

    public DateTimeOffset? NextRetryAt { get; set; }

    public DateTimeOffset? LastAttemptAt { get; set; }

    public string? LastErrorCode { get; set; }

    public string? LastErrorMessage { get; set; }

    public string? FailureCode { get; set; }

    public string? FailureMessage { get; set; }

    public string? RawRequestJson { get; set; }

    public string? RawResponseJson { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    public void MarkAttempted(DateTimeOffset attemptedAt)
    {
        if (Status is PaymentTransactionStatus.Paid or PaymentTransactionStatus.Cancelled or PaymentTransactionStatus.Refunded)
        {
            throw new DomainRuleException("Cannot retry a finalized payment transaction.");
        }

        LastAttemptAt = attemptedAt;
    }

    public void Authorize(string? providerTransactionId, DateTimeOffset authorizedAt)
    {
        if (Status is PaymentTransactionStatus.Cancelled or PaymentTransactionStatus.Failed or PaymentTransactionStatus.Refunded)
        {
            throw new DomainRuleException("Cannot authorize a finalized payment transaction.");
        }

        ProviderTransactionId = providerTransactionId ?? ProviderTransactionId;
        AuthorizedAt = authorizedAt;
        Status = PaymentTransactionStatus.Authorized;
        ClearRetryState();
    }

    public void MarkPaid(string? providerTransactionId, DateTimeOffset paidAt, string? rawResponseJson = null)
    {
        if (Amount <= 0)
        {
            throw new DomainRuleException("Payment amount must be greater than zero.");
        }

        if (Status == PaymentTransactionStatus.Refunded)
        {
            throw new DomainRuleException("Cannot mark a refunded payment as paid.");
        }

        ProviderTransactionId = providerTransactionId ?? ProviderTransactionId;
        PaidAt = paidAt;
        RawResponseJson = rawResponseJson ?? RawResponseJson;
        Status = PaymentTransactionStatus.Paid;
        ClearRetryState();
    }

    public void MarkFailed(string failureCode, string failureMessage, DateTimeOffset failedAt)
    {
        if (Status == PaymentTransactionStatus.Paid)
        {
            throw new DomainRuleException("Cannot fail a paid transaction.");
        }

        FailureCode = failureCode;
        FailureMessage = failureMessage;
        LastErrorCode = failureCode;
        LastErrorMessage = failureMessage;
        FailedAt = failedAt;
        Status = PaymentTransactionStatus.Failed;
    }

    public void ScheduleRetry(string errorCode, string errorMessage, DateTimeOffset nextRetryAt)
    {
        if (!CanRetry)
        {
            throw new DomainRuleException("Payment transaction retry limit has been reached.");
        }

        RetryCount++;
        LastErrorCode = errorCode;
        LastErrorMessage = errorMessage;
        NextRetryAt = nextRetryAt;
        Status = PaymentTransactionStatus.Pending;
    }

    public void Cancel(DateTimeOffset cancelledAt)
    {
        if (Status == PaymentTransactionStatus.Cancelled)
        {
            return;
        }

        if (Status is PaymentTransactionStatus.Paid or PaymentTransactionStatus.Refunded)
        {
            throw new DomainRuleException("Cannot cancel a paid or refunded transaction.");
        }

        CancelledAt = cancelledAt;
        Status = PaymentTransactionStatus.Cancelled;
        ClearRetryState();
    }

    public void AssignPrimarySettlement()
    {
        if (Status != PaymentTransactionStatus.Paid)
        {
            throw new DomainRuleException("Only a paid transaction can become the primary settlement.");
        }

        if (SettlementDisposition is not (PaymentSettlementDisposition.Unassigned or PaymentSettlementDisposition.Primary))
        {
            throw new DomainRuleException("A duplicate payment cannot become the primary settlement.");
        }

        SettlementDisposition = PaymentSettlementDisposition.Primary;
    }

    public void MarkDuplicateRefundRequired(string reason)
    {
        if (Status != PaymentTransactionStatus.Paid)
        {
            throw new DomainRuleException("Only a paid transaction can require duplicate-payment intervention.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleException("Duplicate-payment intervention reason is required.");
        }

        if (SettlementDisposition is PaymentSettlementDisposition.Primary or PaymentSettlementDisposition.DuplicateResolved)
        {
            throw new DomainRuleException("The payment settlement disposition cannot be changed to an unresolved duplicate.");
        }

        SettlementDisposition = PaymentSettlementDisposition.DuplicateRefundRequired;
        LastErrorCode = "DUPLICATE_PAYMENT_REFUND_REQUIRED";
        LastErrorMessage = reason.Trim();
    }

    public void ResolveDuplicatePayment(bool moneyWasRefunded)
    {
        if (SettlementDisposition != PaymentSettlementDisposition.DuplicateRefundRequired)
        {
            throw new DomainRuleException("Only an unresolved duplicate payment can be resolved.");
        }

        if (moneyWasRefunded)
        {
            Status = PaymentTransactionStatus.Refunded;
        }

        SettlementDisposition = PaymentSettlementDisposition.DuplicateResolved;
        LastErrorCode = null;
        LastErrorMessage = null;
    }

    public void MarkExpired(DateTimeOffset expiredAt)
    {
        if (Status == PaymentTransactionStatus.Expired)
        {
            return;
        }

        if (Status is PaymentTransactionStatus.Paid or PaymentTransactionStatus.Refunded)
        {
            throw new DomainRuleException("Cannot expire a paid or refunded transaction.");
        }

        CancelledAt = expiredAt;
        Status = PaymentTransactionStatus.Expired;
        ClearRetryState();
    }

    public bool CanRetry => Status is PaymentTransactionStatus.Pending or PaymentTransactionStatus.Failed
        && RetryCount < MaxRetries;

    private void ClearRetryState()
    {
        NextRetryAt = null;
        LastErrorCode = null;
        LastErrorMessage = null;
    }
}
