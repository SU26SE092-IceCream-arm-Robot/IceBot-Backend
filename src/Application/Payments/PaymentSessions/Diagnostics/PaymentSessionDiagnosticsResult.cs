using Domain.Payments.Enums;

namespace Application.Payments.PaymentSessions.Diagnostics;

public sealed record PaymentSessionDiagnosticsResult(
    Guid PaymentTransactionId,
    Guid OrderId,
    string Provider,
    string? ProviderOrderCode,
    string? ProviderPaymentLinkId,
    string? ProviderTransactionId,
    PaymentTransactionStatus Status,
    decimal Amount,
    decimal? PaidAmount,
    string Currency,
    string? ProviderStatus,
    DateTimeOffset RequestedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset? NextRetryAt,
    int RetryCount,
    int MaxRetries,
    string? LastErrorCode,
    string? LastErrorMessage,
    string? RawRequestJson,
    string? RawResponseJson);
