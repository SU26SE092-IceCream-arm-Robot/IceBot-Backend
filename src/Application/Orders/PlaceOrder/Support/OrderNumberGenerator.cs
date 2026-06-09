using System;

namespace Application.Orders.PlaceOrder.Support;

internal static class OrderNumberGenerator
{
    public static string GenerateOrderNumber(DateTimeOffset now)
    {
        return $"ORD-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..36].ToUpperInvariant();
    }
}
