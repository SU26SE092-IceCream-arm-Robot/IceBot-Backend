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
}
