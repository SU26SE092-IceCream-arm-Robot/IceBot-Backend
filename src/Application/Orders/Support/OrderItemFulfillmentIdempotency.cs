using System.Security.Cryptography;
using System.Text;

namespace Application.Orders.Support;

internal static class OrderItemFulfillmentIdempotency
{
    public static string ComputePayloadHash(string eventType, string? reason)
    {
        var normalizedReason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        var payload = $"{eventType.Trim().ToUpperInvariant()}\n{normalizedReason}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }
}
