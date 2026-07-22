namespace Application.Payments.PaymentSessions.Commands;

public sealed record ReconcilePendingPaymentSessionCommand(
    Guid PaymentTransactionId,
    DateTimeOffset ObservedAt,
    DateTimeOffset NextRetryAt);

public enum PaymentSessionReconciliationOutcome
{
    Skipped = 0,
    Restored = 1,
    NotFound = 2,
    Cancelled = 3,
    Expired = 4,
    AwaitingWebhook = 5,
    RetryScheduled = 6,
    RetryExhausted = 7,
    AmountMismatch = 8,
    IdentityMismatch = 9
}
