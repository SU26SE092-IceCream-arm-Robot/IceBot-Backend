namespace Application.Payments.Providers;

public sealed class ProviderPaymentSession
{
    public string? ProviderOrderCode { get; init; }

    public string? ProviderPaymentLinkId { get; init; }

    public string? ProviderTransactionId { get; init; }

    public string? CheckoutUrl { get; init; }

    public string? QrCodePayload { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }

    public string? ProviderStatus { get; init; }

    public decimal? Amount { get; init; }

    public decimal? PaidAmount { get; init; }

    public string RawResponseJson { get; init; } = "{}";
}
