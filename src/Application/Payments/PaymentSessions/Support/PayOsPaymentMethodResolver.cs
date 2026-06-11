using Application.Payments.Abstractions;
using Domain.Payments.Entities;

namespace Application.Payments.PaymentSessions.Support;

internal static class PayOsPaymentMethodResolver
{
    private const string PayOsMethodCode = "payos";

    public static async Task<PaymentMethod> EnsurePayOsPaymentMethodAsync(
        IPaymentStore paymentStore,
        string providerCode,
        CancellationToken cancellationToken)
    {
        var paymentMethod = await paymentStore.GetPaymentMethodByCodeAsync(PayOsMethodCode, cancellationToken);
        if (paymentMethod is not null)
        {
            return paymentMethod;
        }

        paymentMethod = new PaymentMethod
        {
            Code = PayOsMethodCode,
            Name = "PayOS",
            Description = "PayOS payment gateway",
            Provider = providerCode,
            MethodType = "BankTransferQr",
            IsOnline = true,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await paymentStore.AddPaymentMethodAsync(paymentMethod, cancellationToken);
        await paymentStore.SaveChangesAsync(cancellationToken);
        return paymentMethod;
    }
}
