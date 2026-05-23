namespace Application.Payments.Providers;

public sealed class ProviderPaymentNotification
{
    public string Provider { get; init; } = null!;

    public string EventType { get; init; } = null!;

    public string? ProviderEventId { get; init; }

    public string? ProviderOrderCode { get; init; }

    public string? ProviderPaymentLinkId { get; init; }

    public string? ProviderTransactionId { get; init; }

    public string ProviderStatus { get; init; } = null!;

    public bool IsPaid { get; init; }

    public bool IsCancelled { get; init; }

    public bool IsExpired { get; init; }

    public decimal? PaidAmount { get; init; }

    public DateTimeOffset? ProviderPaidAt { get; init; }

    public string RawPayloadJson { get; init; } = "{}";
}
