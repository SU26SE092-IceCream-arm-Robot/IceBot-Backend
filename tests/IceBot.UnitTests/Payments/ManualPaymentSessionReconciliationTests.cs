using Application.Operations.OperationLogs.Abstractions;
using Application.Payments.Abstractions;
using Application.Payments.PaymentSessions.Commands;
using Application.Payments.PaymentSessions.Notifications;
using Application.Payments.Providers;
using Application.Identity.Tokens.Claims;
using Domain.Orders.Entities;
using Domain.Operations.Entities;
using Domain.Payments.Entities;
using Domain.Payments.Enums;
using NSubstitute;

namespace IceBot.UnitTests.Payments;

public sealed class ManualPaymentSessionReconciliationTests
{
    [Fact]
    public async Task EligibleSession_RestoresInstructionsAndWritesRequestAndResultAudit()
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORDER-1",
            KioskId = Guid.NewGuid()
        };
        var payment = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            Provider = "PayOS",
            ProviderOrderCode = "1234567890123",
            Amount = 30_000,
            Currency = "VND",
            Status = PaymentTransactionStatus.Pending,
            RequestedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            LastErrorCode = "PROVIDER_SESSION_CREATE_OUTCOME_UNKNOWN"
        };
        var store = Substitute.For<IPaymentStore>();
        store.GetPaymentTransactionByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        store.GetPaymentTransactionSnapshotAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<PaymentSessionReconciliationOutcome>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<PaymentSessionReconciliationOutcome>>>()(
                CancellationToken.None));
        var gateway = Substitute.For<IPaymentGateway>();
        gateway.GetPaymentSessionAsync(payment.ProviderOrderCode, Arg.Any<CancellationToken>())
            .Returns(new ProviderPaymentSession
            {
                ProviderOrderCode = payment.ProviderOrderCode,
                CheckoutUrl = "https://pay.test/session",
                ProviderStatus = "PENDING",
                Amount = payment.Amount
            });
        var operationLogs = Substitute.For<IOperationLogStore>();
        var handler = new ManuallyReconcilePaymentSessionCommandHandler(
            store,
            operationLogs,
            new ReconcilePendingPaymentSessionCommandHandler(
                store, gateway, Substitute.For<IPaymentInterventionNotifier>()));

        var result = await handler.HandleAsync(new ManuallyReconcilePaymentSessionCommand(
            order.Id,
            payment.Id,
            "Provider response was lost",
            CreateManagerContext(order)));

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(PaymentSessionReconciliationOutcome.Restored, result.Data!.Outcome);
        Assert.Equal("https://pay.test/session", payment.CheckoutUrl);
        await operationLogs.Received(2).AddAsync(
            Arg.Is<OperationLog>(log => log.Category == "Payment" && log.OrderId == order.Id),
            Arg.Any<CancellationToken>());
        await operationLogs.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransactionOutsideOrder_IsRejectedWithoutProviderLookupOrAudit()
    {
        var payment = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Order = new Order { Id = Guid.NewGuid(), KioskId = Guid.NewGuid() },
            Provider = "PayOS",
            ProviderOrderCode = "1234567890123",
            Status = PaymentTransactionStatus.Pending
        };
        var store = Substitute.For<IPaymentStore>();
        store.GetPaymentTransactionByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        var gateway = Substitute.For<IPaymentGateway>();
        var operationLogs = Substitute.For<IOperationLogStore>();
        var handler = new ManuallyReconcilePaymentSessionCommandHandler(
            store,
            operationLogs,
            new ReconcilePendingPaymentSessionCommandHandler(
                store, gateway, Substitute.For<IPaymentInterventionNotifier>()));

        var result = await handler.HandleAsync(new ManuallyReconcilePaymentSessionCommand(
            Guid.NewGuid(),
            payment.Id,
            "Investigate",
            CreateManagerContext(payment.Order)));

        Assert.False(result.Succeeded);
        Assert.Equal(404, result.StatusCode);
        await gateway.DidNotReceive().GetPaymentSessionAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
        await operationLogs.DidNotReceive().AddAsync(
            Arg.Any<OperationLog>(), Arg.Any<CancellationToken>());
    }

    private static CurrentUserContext CreateManagerContext(Order order) => new()
    {
        AccountId = Guid.NewGuid(),
        RoleScopes = [new UserRoleScope("Manager", null, null, order.KioskId)]
    };
}
