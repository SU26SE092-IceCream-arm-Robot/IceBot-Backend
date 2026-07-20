using Application.Abstractions.Realtime;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Dispatch;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.EdgeIntegration.Dispatch.Services;
using Application.Payments.Abstractions;
using Application.Payments.PaymentSessions.Commands;
using Application.Payments.PaymentSessions.Requests;
using Application.Payments.PaymentSessions.Results;
using Application.Payments.Providers;
using Application.Shared.Wrappers;
using Domain.Orders.Entities;
using Domain.Payments.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace IceBot.UnitTests.Payments;

public sealed class HandlePaymentProviderNotificationCommandHandlerTests
{
    [Fact]
    public async Task CallerCancellationDuringWebhookVerification_IsPropagated()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var store = Substitute.For<IPaymentStore>();
        var gateway = Substitute.For<IPaymentGateway>();
        gateway.ParseAndVerifyNotificationAsync(
                Arg.Any<string>(), Arg.Any<string?>(), cancellation.Token)
            .Returns<Task<ProviderPaymentNotification>>(_ =>
                throw new OperationCanceledException(cancellation.Token));
        var handler = CreateHandler(store, gateway);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.HandleAsync(
            new HandlePaymentProviderNotificationCommand
            {
                Request = new HandlePaymentProviderNotificationRequest { RawPayload = "{}" }
            },
            cancellation.Token));
    }

    [Fact]
    public async Task PaidWebhookWithMismatchedAmount_IsRejectedBeforeStateMutation()
    {
        var order = new Order { Id = Guid.NewGuid(), KioskId = Guid.NewGuid() };
        var payment = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            Provider = "payos",
            ProviderOrderCode = "1234567890123",
            Amount = 30_000,
            Currency = "VND"
        };
        var notification = new ProviderPaymentNotification
        {
            Provider = "payos",
            ProviderEventId = "event:underpaid",
            ProviderOrderCode = payment.ProviderOrderCode,
            EventType = "PAID",
            ProviderStatus = "PAID",
            IsPaid = true,
            PaidAmount = 20_000,
            RawPayloadJson = "{}"
        };
        var store = Substitute.For<IPaymentStore>();
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<ApiResult<PaymentNotificationResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<PaymentNotificationResult>>>>()(CancellationToken.None));
        store.PaymentCallbackExistsAsync("payos", notification.ProviderEventId, Arg.Any<CancellationToken>())
            .Returns(false);
        store.GetPaymentTransactionByProviderOrderCodeAsync(
                "payos", payment.ProviderOrderCode, Arg.Any<CancellationToken>())
            .Returns(payment);
        var gateway = Substitute.For<IPaymentGateway>();
        gateway.ParseAndVerifyNotificationAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(notification);
        var dispatchStore = Substitute.For<IOrderExecutionDispatchStore>();
        var handler = CreateHandler(store, gateway, dispatchStore);

        var result = await handler.HandleAsync(new HandlePaymentProviderNotificationCommand
        {
            Request = new HandlePaymentProviderNotificationRequest { RawPayload = "{}" }
        });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("does not match", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(Domain.Payments.Enums.PaymentTransactionStatus.Pending, payment.Status);
        await store.DidNotReceive().AddPaymentCallbackAsync(
            Arg.Any<PaymentCallback>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await dispatchStore.DidNotReceive().ExecuteSerializedAsync(
            Arg.Any<Guid>(),
            Arg.Any<Func<CancellationToken, Task<ApiResult<Application.EdgeIntegration.Dispatch.Results.OrderExecutionDispatchResult>>>>(),
            Arg.Any<CancellationToken>());
    }

    private static HandlePaymentProviderNotificationCommandHandler CreateHandler(
        IPaymentStore store,
        IPaymentGateway gateway,
        IOrderExecutionDispatchStore? dispatchStore = null)
    {
        var dispatchHandler = new DispatchOrderExecutionCommandHandler(
            dispatchStore ?? Substitute.For<IOrderExecutionDispatchStore>(),
            Options.Create(new OrderExecutionDispatchOptions { Enabled = true }),
            Substitute.For<IEdgeCommandWakeUpPublisher>());
        return new HandlePaymentProviderNotificationCommandHandler(
            store,
            gateway,
            Substitute.For<IRealtimeNotificationPublisher>(),
            dispatchHandler,
            NullLogger<HandlePaymentProviderNotificationCommandHandler>.Instance);
    }
}
