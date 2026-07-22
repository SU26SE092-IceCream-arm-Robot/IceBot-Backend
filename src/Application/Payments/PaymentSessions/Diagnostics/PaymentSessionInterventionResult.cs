using Domain.Payments.Enums;

namespace Application.Payments.PaymentSessions.Diagnostics;

public sealed record PaymentSessionInterventionResult(
    Guid PaymentTransactionId,
    Guid OrderId,
    string OrderNumber,
    Guid? OrganizationId,
    Guid? StoreId,
    Guid KioskId,
    string Provider,
    string ProviderOrderCode,
    PaymentTransactionStatus Status,
    decimal Amount,
    string Currency,
    string InterventionCode,
    string? InterventionMessage,
    int RetryCount,
    int MaxRetries,
    DateTimeOffset RequestedAt,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset? NextRetryAt,
    bool CanReconcile);
