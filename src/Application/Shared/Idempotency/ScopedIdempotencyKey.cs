namespace Application.Shared.Idempotency;

public static class ScopedIdempotencyKey
{
    public const int MaxClientKeyLength = 128;

    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = value?.Trim() ?? string.Empty;
        return normalized.Length is > 0 and <= MaxClientKeyLength;
    }

    public static string ForKiosk(Guid kioskId, string clientKey) =>
        $"kiosk:{kioskId:N}:{clientKey}";

    public static string ForOrder(Guid orderId, string clientKey) =>
        $"order:{orderId:N}:{clientKey}";

    public static string ForPaymentTransaction(Guid paymentTransactionId, string clientKey) =>
        $"payment-transaction:{paymentTransactionId:N}:{clientKey}";
}
