using Domain.Payments.Entities;
using Domain.Payments.Enums;

namespace IceBot.UnitTests.Payments;

public sealed class PaymentTransactionLifecycleTests
{
    [Fact]
    public void MarkPaid_AfterCancellation_AcceptsAuthoritativeProviderPayment()
    {
        var transaction = new PaymentTransaction
        {
            Amount = 10_000,
            Status = PaymentTransactionStatus.Cancelled
        };

        transaction.MarkPaid("provider-transaction", DateTimeOffset.UtcNow);

        Assert.Equal(PaymentTransactionStatus.Paid, transaction.Status);
    }
}
