using Application.Abstractions.Realtime;
using Application.Identity.Tokens.Claims;
using Application.Payments.Abstractions;
using Application.Payments.Refunds.Commands;
using Application.Payments.Refunds.Results;
using Application.Shared.Wrappers;
using Domain.Orders.Entities;
using Domain.Payments.Entities;
using Domain.Payments.Enums;
using NSubstitute;

namespace IceBot.UnitTests.Payments;

public sealed class RefundConfirmationTests
{
    [Fact]
    public async Task RefundRequest_DefaultsToTheSingleDuplicatePaymentInsteadOfLatestSession()
    {
        const decimal amount = 10_000;
        var order = CreatePaidOrder(amount);
        var primary = CreatePaidTransaction(order, amount, "PRIMARY");
        primary.AssignPrimarySettlement();
        primary.RequestedAt = DateTimeOffset.UtcNow;
        var duplicate = CreatePaidTransaction(order, amount, "OLDER-DUPLICATE");
        duplicate.RequestedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        duplicate.MarkDuplicateRefundRequired("Duplicate provider payment.");
        order.MarkDuplicatePaymentRefundRequired("Duplicate provider payment.");

        Refund? createdRefund = null;
        var store = Substitute.For<IPaymentStore>();
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<ApiResult<RefundResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<RefundResult>>>>()(CancellationToken.None));
        store.GetOrderByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        store.ListPaymentTransactionsByOrderIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { primary, duplicate });
        store.AddRefundAsync(Arg.Do<Refund>(candidate =>
        {
            candidate.PaymentTransaction = duplicate;
            createdRefund = candidate;
        }), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        store.GetRefundByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => createdRefund);

        var result = await new RequestRefundCommandHandler(
                store,
                Substitute.For<IRealtimeNotificationPublisher>())
            .HandleAsync(new RequestRefundCommand
            {
                OrderId = order.Id,
                UserContext = new CurrentUserContext { IsSystemAdmin = true },
                RefundMethod = "FullMoneyRefund",
                Reason = "Duplicate payment",
                IdempotencyKey = "refund-duplicate"
            });

        Assert.True(result.Succeeded, result.Message);
        Assert.NotNull(createdRefund);
        Assert.Equal(duplicate.Id, createdRefund!.PaymentTransactionId);
    }

    [Fact]
    public async Task RefundRequest_CannotTargetPrimaryWhileDuplicatePaymentIsUnresolved()
    {
        const decimal amount = 10_000;
        var order = CreatePaidOrder(amount);
        var primary = CreatePaidTransaction(order, amount, "PRIMARY");
        primary.AssignPrimarySettlement();
        var duplicate = CreatePaidTransaction(order, amount, "DUPLICATE");
        duplicate.MarkDuplicateRefundRequired("Duplicate provider payment.");
        order.MarkDuplicatePaymentRefundRequired("Duplicate provider payment.");
        var store = Substitute.For<IPaymentStore>();
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<ApiResult<RefundResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<RefundResult>>>>()(CancellationToken.None));
        store.GetOrderByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        store.ListPaymentTransactionsByOrderIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { primary, duplicate });

        var result = await new RequestRefundCommandHandler(
                store,
                Substitute.For<IRealtimeNotificationPublisher>())
            .HandleAsync(new RequestRefundCommand
            {
                OrderId = order.Id,
                PaymentTransactionId = primary.Id,
                UserContext = new CurrentUserContext { IsSystemAdmin = true },
                RefundMethod = "FullMoneyRefund",
                Reason = "Wrong target",
                IdempotencyKey = "refund-primary"
            });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        await store.DidNotReceive().AddRefundAsync(Arg.Any<Refund>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DuplicatePaymentRefund_RestoresSettlementAndPriorOrderState()
    {
        const decimal amount = 10_000;
        var order = CreatePaidOrder(amount);
        var primary = CreatePaidTransaction(order, amount, "PRIMARY");
        primary.AssignPrimarySettlement();
        var duplicate = CreatePaidTransaction(order, amount, "DUPLICATE");
        duplicate.MarkDuplicateRefundRequired("Duplicate provider payment.");
        order.MarkDuplicatePaymentRefundRequired("Duplicate provider payment.");
        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            PaymentTransactionId = duplicate.Id,
            PaymentTransaction = duplicate,
            RefundNumber = "REFUND-DUPLICATE",
            Amount = amount,
            Currency = "VND",
            Reason = "{\"Method\":\"FullMoneyRefund\",\"Text\":\"Duplicate payment\"}",
            Status = RefundStatus.Requested
        };

        var store = Substitute.For<IPaymentStore>();
        store.GetRefundByIdAsync(refund.Id, Arg.Any<CancellationToken>()).Returns(refund);
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<ApiResult<RefundResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<RefundResult>>>>()(CancellationToken.None));
        store.HasOtherUnresolvedDuplicatePaymentsAsync(order.Id, duplicate.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await new MarkRefundProcessedCommandHandler(
                store,
                Substitute.For<IRealtimeNotificationPublisher>())
            .HandleAsync(new MarkRefundProcessedCommand
            {
                RefundId = refund.Id,
                UserContext = new CurrentUserContext { IsSystemAdmin = true },
                MoneyWasRefunded = true
            });

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(PaymentTransactionStatus.Paid, primary.Status);
        Assert.Equal(PaymentTransactionStatus.Refunded, duplicate.Status);
        Assert.Equal(PaymentSettlementDisposition.DuplicateResolved, duplicate.SettlementDisposition);
        Assert.Equal(Domain.Orders.Enums.PaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(Domain.Orders.Enums.OrderStatus.ReadyForFulfillment, order.Status);
        Assert.Equal(amount, order.PaidAmount);
    }

    [Fact]
    public async Task DuplicatePaymentRefund_KeepsOrderBlockedWhileAnotherDuplicateIsUnresolved()
    {
        const decimal amount = 10_000;
        var order = CreatePaidOrder(amount);
        var duplicate = CreatePaidTransaction(order, amount, "DUPLICATE-1");
        duplicate.MarkDuplicateRefundRequired("Duplicate provider payment.");
        order.MarkDuplicatePaymentRefundRequired("Duplicate provider payment.");
        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            PaymentTransactionId = duplicate.Id,
            PaymentTransaction = duplicate,
            RefundNumber = "REFUND-DUPLICATE-1",
            Amount = amount,
            Currency = "VND",
            Reason = "{\"Method\":\"FullMoneyRefund\",\"Text\":\"Duplicate payment\"}",
            Status = RefundStatus.Requested
        };
        var store = Substitute.For<IPaymentStore>();
        store.GetRefundByIdAsync(refund.Id, Arg.Any<CancellationToken>()).Returns(refund);
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<ApiResult<RefundResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<RefundResult>>>>()(CancellationToken.None));
        store.HasOtherUnresolvedDuplicatePaymentsAsync(order.Id, duplicate.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await new MarkRefundProcessedCommandHandler(
                store,
                Substitute.For<IRealtimeNotificationPublisher>())
            .HandleAsync(new MarkRefundProcessedCommand
            {
                RefundId = refund.Id,
                UserContext = new CurrentUserContext { IsSystemAdmin = true },
                MoneyWasRefunded = true
            });

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(Domain.Orders.Enums.OrderStatus.RefundRequired, order.Status);
    }

    [Fact]
    public async Task FullMoneyRefund_RequiresExplicitMoneyConfirmationBeforeMutation()
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORDER-1",
            KioskId = Guid.NewGuid()
        };
        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            Provider = "manual",
            Status = PaymentTransactionStatus.Paid
        };
        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            PaymentTransactionId = transaction.Id,
            PaymentTransaction = transaction,
            RefundNumber = "REFUND-1",
            Amount = 10_000,
            Reason = "Customer request",
            Status = RefundStatus.Requested
        };

        var store = Substitute.For<IPaymentStore>();
        store.GetRefundByIdAsync(refund.Id, Arg.Any<CancellationToken>()).Returns(refund);
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<ApiResult<RefundResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<RefundResult>>>>()(CancellationToken.None));

        var handler = new MarkRefundProcessedCommandHandler(
            store,
            Substitute.For<IRealtimeNotificationPublisher>());
        var result = await handler.HandleAsync(new MarkRefundProcessedCommand
        {
            RefundId = refund.Id,
            UserContext = new CurrentUserContext { IsSystemAdmin = true }
        });

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(RefundStatus.Requested, refund.Status);
        Assert.Null(refund.ProcessedAt);
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FullMoneyRefund_RejectsExplicitFalseConfirmation()
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORDER-2",
            KioskId = Guid.NewGuid()
        };
        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            Provider = "manual",
            Status = PaymentTransactionStatus.Paid
        };
        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            PaymentTransactionId = transaction.Id,
            PaymentTransaction = transaction,
            RefundNumber = "REFUND-2",
            Amount = 10_000,
            Reason = "Customer request",
            Status = RefundStatus.Requested
        };
        var store = Substitute.For<IPaymentStore>();
        store.GetRefundByIdAsync(refund.Id, Arg.Any<CancellationToken>()).Returns(refund);
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<ApiResult<RefundResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<RefundResult>>>>()(CancellationToken.None));
        var handler = new MarkRefundProcessedCommandHandler(
            store,
            Substitute.For<IRealtimeNotificationPublisher>());

        var result = await handler.HandleAsync(new MarkRefundProcessedCommand
        {
            RefundId = refund.Id,
            UserContext = new CurrentUserContext { IsSystemAdmin = true },
            MoneyWasRefunded = false
        });

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(RefundStatus.Requested, refund.Status);
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static Order CreatePaidOrder(decimal amount)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"ORDER-{Guid.NewGuid():N}",
            KioskId = Guid.NewGuid()
        };
        order.SetCurrency("VND");
        order.AddItem(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            "ITEM", "Item", "PRODUCT", "Product", "VARIANT", "Variant", null,
            Domain.Catalog.Enums.FulfillmentType.MachineProduced, 1, amount);
        var placedAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        order.Place(placedAt, placedAt.AddMinutes(15));
        order.MarkPaid(amount, DateTimeOffset.UtcNow.AddMinutes(-1));
        return order;
    }

    private static PaymentTransaction CreatePaidTransaction(Order order, decimal amount, string code)
    {
        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            Provider = "manual",
            ProviderOrderCode = code,
            Amount = amount,
            Currency = "VND",
            Status = PaymentTransactionStatus.Pending
        };
        transaction.MarkPaid(code, DateTimeOffset.UtcNow);
        return transaction;
    }
}
