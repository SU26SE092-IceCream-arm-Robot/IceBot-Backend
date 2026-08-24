using Application.Payments.Abstractions;
using Application.Payments.Options;
using Application.Payments.PaymentSessions.Support;
using Domain.Payments.Entities;
using Domain.Payments.Enums;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace IceBot.UnitTests.Payments;

public sealed class PaymentProviderExchangeCoordinatorTests
{
    [Fact]
    public async Task CompleteAsync_LocksBeforeLoadingTheMutablePaymentProjection()
    {
        var paymentTransactionId = Guid.NewGuid();
        var exchange = PaymentProviderExchange.Start(
            paymentTransactionId,
            "PayOS",
            PaymentProviderExchangeOperation.CreateSession,
            1,
            "123",
            DateTimeOffset.UtcNow);
        var store = Substitute.For<IPaymentStore>();
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<bool>>>()(CancellationToken.None));
        store.GetPaymentProviderExchangePaymentTransactionIdAsync(exchange.Id, Arg.Any<CancellationToken>())
            .Returns(paymentTransactionId);
        store.GetPaymentProviderExchangeByIdAsync(exchange.Id, Arg.Any<CancellationToken>())
            .Returns(exchange);
        var coordinator = new PaymentProviderExchangeCoordinator(
            store,
            Options.Create(new PaymentProviderExchangeOptions()),
            TimeProvider.System);

        var completed = await coordinator.CompleteAsync(
            exchange.Id,
            PaymentProviderExchangeOutcome.Succeeded,
            null,
            null,
            null,
            null);

        Assert.True(completed);
        Received.InOrder(() =>
        {
            _ = store.GetPaymentProviderExchangePaymentTransactionIdAsync(exchange.Id, Arg.Any<CancellationToken>());
            _ = store.AcquirePaymentTransactionLockAsync(paymentTransactionId, Arg.Any<CancellationToken>());
            _ = store.GetPaymentProviderExchangeByIdAsync(exchange.Id, Arg.Any<CancellationToken>());
        });
    }
}
