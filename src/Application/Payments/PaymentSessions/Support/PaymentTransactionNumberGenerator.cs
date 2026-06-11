namespace Application.Payments.PaymentSessions.Support;

internal static class PaymentTransactionNumberGenerator
{
    public static string GenerateTransactionNumber(DateTimeOffset now)
    {
        return $"PAY-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..36].ToUpperInvariant();
    }
}
