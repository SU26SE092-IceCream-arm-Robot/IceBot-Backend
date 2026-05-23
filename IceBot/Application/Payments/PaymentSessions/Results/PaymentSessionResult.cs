using Domain.Payments.Enums;

namespace Application.Payments.PaymentSessions.Results;

public sealed class PaymentSessionResult
{
    public Guid PaymentTransactionId { get; set; }

    public Guid OrderId { get; set; }

    public string TransactionNumber { get; set; } = null!;

    public string Provider { get; set; } = null!;

    public string? ProviderOrderCode { get; set; }

    public string? ProviderPaymentLinkId { get; set; }

    public string? CheckoutUrl { get; set; }

    public string? QrCodePayload { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "VND";

    public PaymentTransactionStatus Status { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }
}
