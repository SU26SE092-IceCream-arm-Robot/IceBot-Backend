using Application.Payments.PaymentSessions.Support;
using Domain.Payments.Entities;
using Domain.Payments.Enums;

namespace IceBot.UnitTests.Payments;

public sealed class PaymentSessionInterventionPolicyTests
{
    [Fact]
    public void ExpiredPendingSession_WithPersistedCheckoutInstructions_CanBeReconciled()
    {
        var observedAt = DateTimeOffset.UtcNow;
        var payment = CreatePendingPayment(observedAt.AddMinutes(-1), "https://checkout.example/session");

        Assert.True(PaymentSessionInterventionPolicy.CanReconcile(payment, observedAt));
    }

    [Fact]
    public void ActivePendingSession_WithCheckoutInstructions_CannotBeReconciled()
    {
        var observedAt = DateTimeOffset.UtcNow;
        var payment = CreatePendingPayment(observedAt.AddMinutes(5), "https://checkout.example/session");

        Assert.False(PaymentSessionInterventionPolicy.CanReconcile(payment, observedAt));
    }

    [Fact]
    public void PaidDuplicatePayment_IsVisibleForInterventionButCannotBeReconciled()
    {
        var payment = new PaymentTransaction
        {
            Provider = "PayOS",
            ProviderOrderCode = "1234567890123",
            Amount = 30_000,
            Status = PaymentTransactionStatus.Pending
        };
        payment.MarkPaid("provider-transaction", DateTimeOffset.UtcNow);
        payment.MarkDuplicateRefundRequired("Duplicate provider payment.");

        var appearsInQueue = new[] { payment }.AsQueryable()
            .Where(PaymentSessionInterventionPolicy.BuildQueuePredicate(DateTimeOffset.UtcNow))
            .Any();

        Assert.True(appearsInQueue);
        Assert.False(PaymentSessionInterventionPolicy.CanReconcile(payment, DateTimeOffset.UtcNow));
    }

    private static PaymentTransaction CreatePendingPayment(DateTimeOffset expiresAt, string checkoutUrl) =>
        new()
        {
            Provider = "PayOS",
            ProviderOrderCode = "1234567890123",
            Status = PaymentTransactionStatus.Pending,
            CheckoutUrl = checkoutUrl,
            QrCodePayload = "qr-payload",
            ExpiresAt = expiresAt
        };
}
