using Application.Identity.Tokens.Claims;

namespace Application.Payments.PaymentSessions.Commands;

public sealed record ManuallyReconcilePaymentSessionCommand(
    Guid OrderId,
    Guid PaymentTransactionId,
    string Reason,
    CurrentUserContext UserContext);

public sealed record ManualPaymentSessionReconciliationResult(
    Guid PaymentTransactionId,
    Guid OrderId,
    PaymentSessionReconciliationOutcome Outcome,
    string Status,
    string? InterventionCode,
    int RetryCount,
    DateTimeOffset? NextRetryAt);
