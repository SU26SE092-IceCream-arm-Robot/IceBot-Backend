using Application.Payments.Abstractions;
using Domain.Payments.Entities;

namespace Application.Payments.PaymentSessions.Support;

internal static class CashPaymentMethodResolver
{
    public const string MethodCode = "cash";
    public const string ProviderCode = "Cash";

    public static bool IsCash(string? paymentMethodCode) =>
        string.Equals(paymentMethodCode, MethodCode, StringComparison.OrdinalIgnoreCase);

    public static async Task<PaymentMethod?> GetCashPaymentMethodAsync(
        IPaymentStore paymentStore,
        CancellationToken cancellationToken) =>
        await paymentStore.GetPaymentMethodByCodeAsync(MethodCode, cancellationToken);
}
