using Application.Shared.Wrappers;

namespace Application.Payments.PaymentSessions;

public static class PaymentErrors
{
    public static readonly ApiBusinessErrorDefinition IdempotencyKeyInvalid = new("PAYMENT.IDEMPOTENCY_KEY_INVALID", 400, "Idempotency-Key is required and invalid.");
    public static readonly ApiBusinessErrorDefinition IdempotencyConflict = new("PAYMENT.IDEMPOTENCY_CONFLICT", 409, "Idempotency key was already used for a different payment request.");
    public static readonly ApiBusinessErrorDefinition WindowExpired = new("PAYMENT.WINDOW_EXPIRED", 409, "The order payment window has expired.");
    public static readonly ApiBusinessErrorDefinition PreviousSessionFailed = new("PAYMENT.PREVIOUS_SESSION_FAILED", 409, "The previous payment session failed. Retry with a new idempotency key.");
    public static readonly ApiBusinessErrorDefinition SessionCreationInProgress = new("PAYMENT.SESSION_CREATION_IN_PROGRESS", 409, "Payment session creation is already in progress.");
    public static readonly ApiBusinessErrorDefinition OrderAlreadyPaid = new("PAYMENT.ORDER_ALREADY_PAID", 409, "The order has already been paid.");
    public static readonly ApiBusinessErrorDefinition OrderNotPayable = new("PAYMENT.ORDER_NOT_PAYABLE", 409, "The order is not payable.");
    public static readonly ApiBusinessErrorDefinition AmountChanged = new("PAYMENT.AMOUNT_CHANGED", 409, "The payment amount no longer matches the order.");
    public static readonly ApiBusinessErrorDefinition MethodNotConfigured = new("PAYMENT.METHOD_NOT_CONFIGURED", 503, "The requested payment method is not configured.");
    public static readonly ApiBusinessErrorDefinition MethodInactive = new("PAYMENT.METHOD_INACTIVE", 503, "The requested payment method is inactive.");
    public static readonly ApiBusinessErrorDefinition ProviderOutcomeUnknown = new("PAYMENT.PROVIDER_OUTCOME_UNKNOWN", 503, "The payment provider outcome is unknown. Reconciliation is required.");
    public static readonly ApiBusinessErrorDefinition ProviderUnavailable = new("PAYMENT.PROVIDER_UNAVAILABLE", 503, "The payment provider is temporarily unavailable.");
    public static readonly ApiBusinessErrorDefinition ProviderRejected = new("PAYMENT.PROVIDER_REJECTED", 502, "The payment provider rejected the session request.");
    public static readonly ApiBusinessErrorDefinition ReconciliationNotEligible = new("PAYMENT.RECONCILIATION_NOT_ELIGIBLE", 409, "Payment session is not eligible for reconciliation.");
    public static readonly ApiBusinessErrorDefinition WebhookPayloadInvalid = new("PAYMENT.WEBHOOK_PAYLOAD_INVALID", 400, "Payment webhook payload is invalid.");
    public static readonly ApiBusinessErrorDefinition WebhookVerificationFailed = new("PAYMENT.WEBHOOK_VERIFICATION_FAILED", 400, "Payment webhook verification failed.");
    public static readonly ApiBusinessErrorDefinition WebhookConfigurationUnavailable = new("PAYMENT.WEBHOOK_CONFIGURATION_UNAVAILABLE", 503, "Payment webhook verification is temporarily unavailable.");

    public static IReadOnlyList<ApiBusinessErrorDefinition> All { get; } =
        [IdempotencyKeyInvalid, IdempotencyConflict, WindowExpired, PreviousSessionFailed,
         SessionCreationInProgress, OrderAlreadyPaid, OrderNotPayable, AmountChanged,
         MethodNotConfigured, MethodInactive, ProviderOutcomeUnknown, ProviderUnavailable,
         ProviderRejected, ReconciliationNotEligible, WebhookPayloadInvalid,
         WebhookVerificationFailed, WebhookConfigurationUnavailable];
}
