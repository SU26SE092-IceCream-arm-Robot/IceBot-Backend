using Application.Payments.Abstractions;
using Application.Payments.PaymentSessions.Commands;
using Application.Payments.PaymentSessions.Notifications;
using Application.Payments.Providers;
using Domain.Payments.Entities;
using Domain.Payments.Enums;
using NSubstitute;

namespace IceBot.UnitTests.Payments;

public sealed class ReconcilePendingPaymentSessionCommandHandlerTests
{
    [Fact]
    public async Task MissingProviderSession_MarksPendingTransactionFailed()
    {
        var payment = PendingPayment();
        var (handler, store, gateway, _) = Handler(payment);
        gateway.GetPaymentSessionAsync(payment.ProviderOrderCode!, Arg.Any<CancellationToken>())
            .Returns((ProviderPaymentSession?)null);

        var outcome = await handler.HandleAsync(Command(payment.Id));

        Assert.Equal(PaymentSessionReconciliationOutcome.NotFound, outcome);
        Assert.Equal(PaymentTransactionStatus.Failed, payment.Status);
        Assert.Equal("PROVIDER_SESSION_NOT_FOUND", payment.FailureCode);
        await store.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProviderSessionWithQr_RestoresPaymentInstructions()
    {
        var payment = PendingPayment();
        var (handler, _, gateway, _) = Handler(payment);
        gateway.GetPaymentSessionAsync(payment.ProviderOrderCode!, Arg.Any<CancellationToken>())
            .Returns(new ProviderPaymentSession
            {
                ProviderOrderCode = payment.ProviderOrderCode,
                ProviderPaymentLinkId = "link-1",
                QrCodePayload = "qr-restored",
                ProviderStatus = "PENDING",
                Amount = payment.Amount
            });

        var outcome = await handler.HandleAsync(Command(payment.Id));

        Assert.Equal(PaymentSessionReconciliationOutcome.Restored, outcome);
        Assert.Equal(PaymentTransactionStatus.Pending, payment.Status);
        Assert.Equal("qr-restored", payment.QrCodePayload);
        Assert.Equal("link-1", payment.ProviderPaymentLinkId);
    }

    [Fact]
    public async Task ProviderSessionWithDifferentIdentity_MarksPendingTransactionFailed()
    {
        var payment = PendingPayment();
        var (handler, _, gateway, notifier) = Handler(payment);
        gateway.GetPaymentSessionAsync(payment.ProviderOrderCode!, Arg.Any<CancellationToken>())
            .Returns(new ProviderPaymentSession
            {
                ProviderOrderCode = "9999999999999",
                CheckoutUrl = "https://pay.test/wrong-session",
                ProviderStatus = "PENDING",
                Amount = payment.Amount
            });

        var outcome = await handler.HandleAsync(Command(payment.Id));

        Assert.Equal(PaymentSessionReconciliationOutcome.IdentityMismatch, outcome);
        Assert.Equal(PaymentTransactionStatus.Failed, payment.Status);
        Assert.Equal("PROVIDER_SESSION_IDENTITY_MISMATCH", payment.FailureCode);
        await notifier.Received().NotifyIfRequiredAsync(
            payment,
            PaymentSessionReconciliationOutcome.IdentityMismatch,
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProviderPaidState_DoesNotReplaceSignedWebhookAuthority()
    {
        var payment = PendingPayment();
        var (handler, _, gateway, _) = Handler(payment);
        gateway.GetPaymentSessionAsync(payment.ProviderOrderCode!, Arg.Any<CancellationToken>())
            .Returns(new ProviderPaymentSession
            {
                ProviderOrderCode = payment.ProviderOrderCode,
                ProviderStatus = "PAID",
                Amount = payment.Amount,
                PaidAmount = payment.Amount
            });

        var outcome = await handler.HandleAsync(Command(payment.Id));

        Assert.Equal(PaymentSessionReconciliationOutcome.RetryScheduled, outcome);
        Assert.Equal(PaymentTransactionStatus.Pending, payment.Status);
        Assert.Equal("AWAITING_SIGNED_WEBHOOK", payment.LastErrorCode);
        Assert.Equal(1, payment.RetryCount);
    }

    [Fact]
    public async Task ExpiredLocalSession_WithProviderExpired_ExpiresPaymentAndOrder()
    {
        var payment = PendingPayment(withOrder: true);
        payment.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        payment.CheckoutUrl = "https://pay.test/session";
        var (handler, _, gateway, _) = Handler(payment);
        gateway.GetPaymentSessionAsync(payment.ProviderOrderCode!, Arg.Any<CancellationToken>())
            .Returns(new ProviderPaymentSession
            {
                ProviderOrderCode = payment.ProviderOrderCode,
                ProviderStatus = "EXPIRED",
                Amount = payment.Amount
            });

        var outcome = await handler.HandleAsync(Command(payment.Id));

        Assert.Equal(PaymentSessionReconciliationOutcome.Expired, outcome);
        Assert.Equal(PaymentTransactionStatus.Expired, payment.Status);
        Assert.Equal(Domain.Orders.Enums.PaymentStatus.Cancelled, payment.Order.PaymentStatus);
    }

    [Fact]
    public async Task ExpiredLocalSession_WithProviderPending_DoesNotExpireWithoutProviderConfirmation()
    {
        var payment = PendingPayment();
        payment.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        payment.CheckoutUrl = "https://pay.test/session";
        var (handler, _, gateway, _) = Handler(payment);
        gateway.GetPaymentSessionAsync(payment.ProviderOrderCode!, Arg.Any<CancellationToken>())
            .Returns(new ProviderPaymentSession
            {
                ProviderOrderCode = payment.ProviderOrderCode,
                ProviderStatus = "PENDING",
                Amount = payment.Amount,
                CheckoutUrl = payment.CheckoutUrl
            });

        var outcome = await handler.HandleAsync(Command(payment.Id));

        Assert.Equal(PaymentSessionReconciliationOutcome.RetryScheduled, outcome);
        Assert.Equal(PaymentTransactionStatus.Pending, payment.Status);
        Assert.Equal("PROVIDER_EXPIRY_NOT_CONFIRMED", payment.LastErrorCode);
    }

    [Fact]
    public async Task FinalFailedLookup_ReportsRetryExhaustedForOperationalAlerting()
    {
        var payment = PendingPayment();
        payment.RetryCount = payment.MaxRetries - 1;
        var (handler, _, gateway, notifier) = Handler(payment);
        gateway.GetPaymentSessionAsync(payment.ProviderOrderCode!, Arg.Any<CancellationToken>())
            .Returns<Task<ProviderPaymentSession?>>(_ => throw new HttpRequestException("provider unavailable"));

        var outcome = await handler.HandleAsync(Command(payment.Id));

        Assert.Equal(PaymentSessionReconciliationOutcome.RetryExhausted, outcome);
        Assert.Equal(payment.MaxRetries, payment.RetryCount);
        Assert.Equal(PaymentTransactionStatus.Pending, payment.Status);
        await notifier.Received().NotifyIfRequiredAsync(
            payment,
            PaymentSessionReconciliationOutcome.RetryExhausted,
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InterventionEnqueueFailure_PreventsPaymentSave()
    {
        var payment = PendingPayment();
        var (handler, store, gateway, notifier) = Handler(payment);
        gateway.GetPaymentSessionAsync(payment.ProviderOrderCode!, Arg.Any<CancellationToken>())
            .Returns(new ProviderPaymentSession
            {
                ProviderOrderCode = "different-provider-order",
                Amount = payment.Amount
            });
        notifier.NotifyIfRequiredAsync(
                payment,
                PaymentSessionReconciliationOutcome.IdentityMismatch,
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("outbox unavailable"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(Command(payment.Id)));

        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static (
        ReconcilePendingPaymentSessionCommandHandler Handler,
        IPaymentStore Store,
        IPaymentGateway Gateway,
        IPaymentInterventionNotifier Notifier) Handler(PaymentTransaction payment)
    {
        var store = Substitute.For<IPaymentStore>();
        store.GetPaymentTransactionSnapshotAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        store.GetPaymentTransactionByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<PaymentSessionReconciliationOutcome>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<PaymentSessionReconciliationOutcome>>>()(
                CancellationToken.None));
        var gateway = Substitute.For<IPaymentGateway>();
        var notifier = Substitute.For<IPaymentInterventionNotifier>();
        return (new ReconcilePendingPaymentSessionCommandHandler(store, gateway, notifier), store, gateway, notifier);
    }

    private static PaymentTransaction PendingPayment(bool withOrder = false) => new()
    {
        Id = Guid.NewGuid(),
        OrderId = Guid.NewGuid(),
        Provider = "PayOS",
        ProviderOrderCode = "1234567890123",
        Amount = 30_000,
        Currency = "VND",
        Status = PaymentTransactionStatus.Pending,
        RequestedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        Order = withOrder
            ? new Domain.Orders.Entities.Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD-TEST",
                KioskId = Guid.NewGuid()
            }
            : null!
    };

    private static ReconcilePendingPaymentSessionCommand Command(Guid paymentId)
    {
        var now = DateTimeOffset.UtcNow;
        return new ReconcilePendingPaymentSessionCommand(paymentId, now, now.AddSeconds(30));
    }
}
