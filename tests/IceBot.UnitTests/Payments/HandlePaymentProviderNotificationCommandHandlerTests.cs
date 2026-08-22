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
using Domain.Payments.Enums;
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
    public async Task InvalidWebhookSignature_IsRejectedBeforePaymentStoreAccess()
    {
        var store = Substitute.For<IPaymentStore>();
        var gateway = Substitute.For<IPaymentGateway>();
        gateway.ParseAndVerifyNotificationAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<Task<ProviderPaymentNotification>>(_ =>
                throw new InvalidOperationException("Invalid PayOS webhook signature."));

        var result = await CreateHandler(store, gateway).HandleAsync(
            new HandlePaymentProviderNotificationCommand
            {
                Request = new HandlePaymentProviderNotificationRequest { RawPayload = "{\"data\":{}}" }
            });

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Empty(store.ReceivedCalls());
    }

    [Fact]
    public async Task VerifiedUnmatchedWebhook_IsAcknowledgedWithoutPaymentMutationOrDispatch()
    {
        var notification = new ProviderPaymentNotification
        {
            Provider = "PayOS",
            ProviderEventId = "event:verified-unmatched",
            ProviderOrderCode = Guid.NewGuid().ToString("N"),
            EventType = "PAID",
            ProviderStatus = "PAID",
            IsPaid = true,
            PaidAmount = 30_000,
            RawPayloadJson = "{\"verified\":true}"
        };
        var store = Substitute.For<IPaymentStore>();
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<ApiResult<PaymentNotificationResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<PaymentNotificationResult>>>>()(CancellationToken.None));
        store.GetPaymentCallbackAsync(notification.Provider, notification.ProviderEventId, Arg.Any<CancellationToken>())
            .Returns((PaymentCallback?)null);
        store.GetPaymentTransactionByProviderOrderCodeAsync(
                notification.Provider, notification.ProviderOrderCode, Arg.Any<CancellationToken>())
            .Returns((PaymentTransaction?)null);
        var gateway = Substitute.For<IPaymentGateway>();
        gateway.ParseAndVerifyNotificationAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(notification);
        var publisher = Substitute.For<IRealtimeNotificationPublisher>();
        var dispatchStore = Substitute.For<IOrderExecutionDispatchStore>();
        var handler = CreateHandler(store, gateway, dispatchStore, publisher);

        var result = await handler.HandleAsync(new HandlePaymentProviderNotificationCommand
        {
            Request = new HandlePaymentProviderNotificationRequest { RawPayload = notification.RawPayloadJson }
        });

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(200, result.StatusCode);
        Assert.Null(result.Data);
        await store.DidNotReceive().AddPaymentCallbackAsync(Arg.Any<PaymentCallback>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().AddOrderStatusHistoryAsync(Arg.Any<OrderStatusHistory>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Empty(publisher.ReceivedCalls());
        await dispatchStore.DidNotReceive().ExecuteSerializedAsync(
            Arg.Any<Guid>(),
            Arg.Any<Func<CancellationToken, Task<ApiResult<Application.EdgeIntegration.Dispatch.Results.OrderExecutionDispatchResult>>>>(),
            Arg.Any<CancellationToken>());
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
        store.GetPaymentCallbackAsync("payos", notification.ProviderEventId, Arg.Any<CancellationToken>())
            .Returns((PaymentCallback?)null);
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
        await store.Received(1).AddPaymentCallbackAsync(
            Arg.Is<PaymentCallback>(callback =>
                callback.ProcessingStatus == Domain.Payments.Enums.PaymentCallbackProcessingStatus.Ignored &&
                callback.LastError != null &&
                callback.LastError.Contains("does not match", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<CancellationToken>());
        await store.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await dispatchStore.DidNotReceive().ExecuteSerializedAsync(
            Arg.Any<Guid>(),
            Arg.Any<Func<CancellationToken, Task<ApiResult<Application.EdgeIntegration.Dispatch.Results.OrderExecutionDispatchResult>>>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DuplicateProviderEventWithDifferentPayload_IsRejected()
    {
        var payment = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Provider = "payos",
            ProviderOrderCode = "1234567890123",
            Status = PaymentTransactionStatus.Paid
        };
        var callback = new PaymentCallback
        {
            PaymentTransactionId = payment.Id,
            PaymentTransaction = payment,
            Provider = "payos",
            ProviderEventId = "event:paid",
            EventType = "PAID",
            PayloadJson = "{\"amount\":30000}",
            ReceivedAt = DateTimeOffset.UtcNow
        };
        callback.MarkProcessed(DateTimeOffset.UtcNow);
        var store = Substitute.For<IPaymentStore>();
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<ApiResult<PaymentNotificationResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<PaymentNotificationResult>>>>()(CancellationToken.None));
        store.GetPaymentCallbackAsync("payos", callback.ProviderEventId!, Arg.Any<CancellationToken>())
            .Returns(callback);
        var gateway = Substitute.For<IPaymentGateway>();
        gateway.ParseAndVerifyNotificationAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ProviderPaymentNotification
            {
                Provider = "payos",
                ProviderEventId = callback.ProviderEventId,
                ProviderOrderCode = payment.ProviderOrderCode,
                EventType = "PAID",
                IsPaid = true,
                PaidAmount = 30_000,
                RawPayloadJson = "{\"amount\":30001}"
            });

        var result = await CreateHandler(store, gateway).HandleAsync(
            new HandlePaymentProviderNotificationCommand
            {
                Request = new HandlePaymentProviderNotificationRequest { RawPayload = "{}" }
            });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("different payment or payload", result.Message, StringComparison.OrdinalIgnoreCase);
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DuplicateMatchingWebhook_RemainsIdempotentWithoutSecondDispatch()
    {
        var payment = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Provider = "PayOS",
            ProviderOrderCode = Guid.NewGuid().ToString("N"),
            Status = PaymentTransactionStatus.Paid
        };
        const string payload = "{\"status\":\"PAID\"}";
        var callback = new PaymentCallback
        {
            PaymentTransactionId = payment.Id,
            PaymentTransaction = payment,
            Provider = payment.Provider,
            ProviderEventId = "event:duplicate",
            EventType = "PAID",
            PayloadJson = payload,
            ReceivedAt = DateTimeOffset.UtcNow
        };
        callback.MarkProcessed(DateTimeOffset.UtcNow);
        var store = Substitute.For<IPaymentStore>();
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<ApiResult<PaymentNotificationResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<PaymentNotificationResult>>>>()(CancellationToken.None));
        store.GetPaymentCallbackAsync(payment.Provider, callback.ProviderEventId!, Arg.Any<CancellationToken>())
            .Returns(callback);
        var gateway = Substitute.For<IPaymentGateway>();
        gateway.ParseAndVerifyNotificationAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ProviderPaymentNotification
            {
                Provider = payment.Provider,
                ProviderEventId = callback.ProviderEventId,
                ProviderOrderCode = payment.ProviderOrderCode,
                EventType = "PAID",
                IsPaid = true,
                PaidAmount = 30_000,
                RawPayloadJson = payload
            });
        var dispatchStore = Substitute.For<IOrderExecutionDispatchStore>();

        var result = await CreateHandler(store, gateway, dispatchStore).HandleAsync(
            new HandlePaymentProviderNotificationCommand
            {
                Request = new HandlePaymentProviderNotificationRequest { RawPayload = payload }
            });

        Assert.True(result.Succeeded, result.Message);
        Assert.True(result.Data!.AlreadyProcessed);
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await dispatchStore.DidNotReceive().ExecuteSerializedAsync(
            Arg.Any<Guid>(),
            Arg.Any<Func<CancellationToken, Task<ApiResult<Application.EdgeIntegration.Dispatch.Results.OrderExecutionDispatchResult>>>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SecondProviderConfirmedPayment_RequiresManualRefundAndDoesNotDispatch()
    {
        const decimal amount = 30_000;
        var order = new Order { Id = Guid.NewGuid(), KioskId = Guid.NewGuid(), OrderNumber = "ORDER-OVERPAID" };
        order.SetCurrency("VND");
        order.AddItem(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            "ITEM", "Item", "PRODUCT", "Product", "VARIANT", "Variant", null,
            Domain.Catalog.Enums.FulfillmentType.MachineProduced, 1, amount);
        var placedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        order.Place(placedAt, placedAt.AddMinutes(15));
        order.MarkPaid(amount, DateTimeOffset.UtcNow.AddMinutes(-9));
        var previousPayment = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            Provider = "payos",
            ProviderOrderCode = "1111111111111",
            Amount = amount,
            Currency = "VND",
            Status = PaymentTransactionStatus.Paid
        };
        var currentPayment = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            Provider = "payos",
            ProviderOrderCode = "2222222222222",
            Amount = amount,
            Currency = "VND",
            Status = PaymentTransactionStatus.Pending
        };
        var notification = new ProviderPaymentNotification
        {
            Provider = "payos",
            ProviderEventId = "event:second-paid",
            ProviderOrderCode = currentPayment.ProviderOrderCode,
            EventType = "PAID",
            ProviderStatus = "PAID",
            IsPaid = true,
            PaidAmount = amount,
            RawPayloadJson = "{\"paid\":true}"
        };
        var store = Substitute.For<IPaymentStore>();
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<ApiResult<PaymentNotificationResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<PaymentNotificationResult>>>>()(CancellationToken.None));
        store.GetPaymentCallbackAsync("payos", notification.ProviderEventId!, Arg.Any<CancellationToken>())
            .Returns((PaymentCallback?)null);
        store.GetPaymentTransactionByProviderOrderCodeAsync(
                "payos", currentPayment.ProviderOrderCode!, Arg.Any<CancellationToken>())
            .Returns(currentPayment);
        store.GetAppliedPaymentSettlementByOrderIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(previousPayment);
        var gateway = Substitute.For<IPaymentGateway>();
        gateway.ParseAndVerifyNotificationAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(notification);
        var dispatchStore = Substitute.For<IOrderExecutionDispatchStore>();

        var result = await CreateHandler(store, gateway, dispatchStore).HandleAsync(
            new HandlePaymentProviderNotificationCommand
            {
                Request = new HandlePaymentProviderNotificationRequest { RawPayload = "{}" }
            });

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(PaymentTransactionStatus.Paid, currentPayment.Status);
        Assert.Equal(PaymentSettlementDisposition.Primary, previousPayment.SettlementDisposition);
        Assert.Equal(PaymentSettlementDisposition.DuplicateRefundRequired, currentPayment.SettlementDisposition);
        Assert.Equal(amount, order.PaidAmount);
        Assert.Equal(Domain.Orders.Enums.OrderStatus.RefundRequired, order.Status);
        await dispatchStore.DidNotReceive().ExecuteSerializedAsync(
            Arg.Any<Guid>(),
            Arg.Any<Func<CancellationToken, Task<ApiResult<Application.EdgeIntegration.Dispatch.Results.OrderExecutionDispatchResult>>>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifiedPaidWebhookAfterLocalPaymentDeadline_RemainsAuthoritative()
    {
        const decimal amount = 30_000;
        var order = new Order { Id = Guid.NewGuid(), KioskId = Guid.NewGuid(), OrderNumber = "ORDER-LATE-PAID" };
        order.SetCurrency("VND");
        order.AddItem(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            "ITEM", "Item", "PRODUCT", "Product", "VARIANT", "Variant", null,
            Domain.Catalog.Enums.FulfillmentType.Packaged, 1, amount);
        var placedAt = DateTimeOffset.UtcNow.AddMinutes(-20);
        order.Place(placedAt, placedAt.AddMinutes(15));
        var payment = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            Provider = "payos",
            ProviderOrderCode = "3333333333333",
            Amount = amount,
            Currency = "VND",
            Status = PaymentTransactionStatus.Expired
        };
        var notification = new ProviderPaymentNotification
        {
            Provider = "payos",
            ProviderEventId = "event:late-paid",
            ProviderOrderCode = payment.ProviderOrderCode,
            EventType = "PAID",
            ProviderStatus = "PAID",
            IsPaid = true,
            PaidAmount = amount,
            RawPayloadJson = "{\"paid\":true}"
        };
        var store = Substitute.For<IPaymentStore>();
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<ApiResult<PaymentNotificationResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<PaymentNotificationResult>>>>()(CancellationToken.None));
        store.GetPaymentCallbackAsync("payos", notification.ProviderEventId!, Arg.Any<CancellationToken>())
            .Returns((PaymentCallback?)null);
        store.GetPaymentTransactionByProviderOrderCodeAsync(
                "payos", payment.ProviderOrderCode!, Arg.Any<CancellationToken>())
            .Returns(payment);
        store.GetAppliedPaymentSettlementByOrderIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns((PaymentTransaction?)null);
        var gateway = Substitute.For<IPaymentGateway>();
        gateway.ParseAndVerifyNotificationAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(notification);

        var result = await CreateHandler(store, gateway).HandleAsync(
            new HandlePaymentProviderNotificationCommand
            {
                Request = new HandlePaymentProviderNotificationRequest { RawPayload = "{}" }
            });

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(PaymentTransactionStatus.Paid, payment.Status);
        Assert.Equal(PaymentSettlementDisposition.Primary, payment.SettlementDisposition);
        Assert.Equal(Domain.Orders.Enums.PaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(Domain.Orders.Enums.OrderStatus.ReadyForFulfillment, order.Status);
    }

    [Fact]
    public async Task VerifiedLatePaymentWhenAnotherCustomerSessionIsActive_RequiresManualRefundAndDoesNotDispatch()
    {
        const decimal amount = 30_000;
        var order = new Order { Id = Guid.NewGuid(), KioskId = Guid.NewGuid(), OrderNumber = "ORDER-LATE-CONFLICT" };
        order.SetCurrency("VND");
        order.AddItem(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            "ITEM", "Item", "PRODUCT", "Product", "VARIANT", "Variant", null,
            Domain.Catalog.Enums.FulfillmentType.MachineProduced, 1, amount);
        var placedAt = DateTimeOffset.UtcNow.AddMinutes(-20);
        order.Place(placedAt, placedAt.AddMinutes(15));
        var payment = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            Provider = "payos",
            ProviderOrderCode = "4444444444444",
            Amount = amount,
            Currency = "VND",
            Status = PaymentTransactionStatus.Expired
        };
        var notification = new ProviderPaymentNotification
        {
            Provider = "payos",
            ProviderEventId = "event:late-session-conflict",
            ProviderOrderCode = payment.ProviderOrderCode,
            EventType = "PAID",
            ProviderStatus = "PAID",
            IsPaid = true,
            PaidAmount = amount,
            RawPayloadJson = "{\"paid\":true}"
        };
        var store = Substitute.For<IPaymentStore>();
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<ApiResult<PaymentNotificationResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<PaymentNotificationResult>>>>()(CancellationToken.None));
        store.GetPaymentCallbackAsync("payos", notification.ProviderEventId!, Arg.Any<CancellationToken>())
            .Returns((PaymentCallback?)null);
        store.GetPaymentTransactionByProviderOrderCodeAsync(
                "payos", payment.ProviderOrderCode!, Arg.Any<CancellationToken>())
            .Returns(payment);
        store.GetAppliedPaymentSettlementByOrderIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns((PaymentTransaction?)null);
        store.HasActiveCustomerSessionAsync(
                order.KioskId,
                Arg.Any<DateTimeOffset>(),
                order.Id,
                Arg.Any<CancellationToken>())
            .Returns(true);
        var gateway = Substitute.For<IPaymentGateway>();
        gateway.ParseAndVerifyNotificationAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(notification);
        var dispatchStore = Substitute.For<IOrderExecutionDispatchStore>();

        var result = await CreateHandler(store, gateway, dispatchStore).HandleAsync(
            new HandlePaymentProviderNotificationCommand
            {
                Request = new HandlePaymentProviderNotificationRequest { RawPayload = "{}" }
            });

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(PaymentTransactionStatus.Paid, payment.Status);
        Assert.Equal(PaymentSettlementDisposition.Primary, payment.SettlementDisposition);
        Assert.Equal(Domain.Orders.Enums.OrderStatus.RefundRequired, order.Status);
        Assert.Contains("another customer session", order.Notes, StringComparison.OrdinalIgnoreCase);
        await dispatchStore.DidNotReceive().ExecuteSerializedAsync(
            Arg.Any<Guid>(),
            Arg.Any<Func<CancellationToken, Task<ApiResult<Application.EdgeIntegration.Dispatch.Results.OrderExecutionDispatchResult>>>>(),
            Arg.Any<CancellationToken>());
    }

    private static HandlePaymentProviderNotificationCommandHandler CreateHandler(
        IPaymentStore store,
        IPaymentGateway gateway,
        IOrderExecutionDispatchStore? dispatchStore = null,
        IRealtimeNotificationPublisher? publisher = null)
    {
        var dispatchHandler = new DispatchOrderExecutionCommandHandler(
            dispatchStore ?? Substitute.For<IOrderExecutionDispatchStore>(),
            Options.Create(new OrderExecutionDispatchOptions { Enabled = true }),
            Substitute.For<IEdgeCommandWakeUpPublisher>());
        return new HandlePaymentProviderNotificationCommandHandler(
            store,
            gateway,
            publisher ?? Substitute.For<IRealtimeNotificationPublisher>(),
            dispatchHandler,
            NullLogger<HandlePaymentProviderNotificationCommandHandler>.Instance);
    }
}
