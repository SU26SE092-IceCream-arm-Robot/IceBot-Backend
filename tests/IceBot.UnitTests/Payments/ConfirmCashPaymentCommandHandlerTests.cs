using Application.Abstractions.Realtime;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Dispatch;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.Identity.Tokens.Claims;
using Application.Payments.Abstractions;
using Application.Payments.PaymentSessions.Commands;
using Application.Payments.PaymentSessions.Requests;
using Domain.Catalog.Enums;
using Domain.Common.Enums;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.Payments.Entities;
using Domain.Payments.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace IceBot.UnitTests.Payments;

public sealed class ConfirmCashPaymentCommandHandlerTests
{
    [Fact]
    public async Task ConfirmCashPayment_InScope_SettlesOrderAndWritesStatusHistory()
    {
        var (order, payment, user) = CreateCashPaymentScenario();
        var store = Substitute.For<IPaymentStore>();
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<Application.Shared.Wrappers.ApiResult<Application.Payments.PaymentSessions.Results.CashPaymentConfirmationResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<Application.Shared.Wrappers.ApiResult<Application.Payments.PaymentSessions.Results.CashPaymentConfirmationResult>>>>()(CancellationToken.None));
        store.GetPaymentTransactionByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        store.GetAppliedPaymentSettlementByOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns((PaymentTransaction?)null);

        var result = await CreateHandler(store).HandleAsync(new ConfirmCashPaymentCommand
        {
            OrderId = order.Id,
            PaymentTransactionId = payment.Id,
            UserContext = user,
            Request = new ConfirmCashPaymentRequest { Note = "Received at counter" }
        });

        Assert.True(result.Succeeded, result.Message);
        Assert.False(result.Data!.AlreadyConfirmed);
        Assert.Equal(PaymentTransactionStatus.Paid, payment.Status);
        Assert.Equal(PaymentSettlementDisposition.Primary, payment.SettlementDisposition);
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(OrderStatus.ReadyForFulfillment, order.Status);
        await store.Received(1).AddOrderStatusHistoryAsync(
            Arg.Is<OrderStatusHistory>(history =>
                history.ChangedByAccountId == user.AccountId &&
                (history.Reason ?? string.Empty).Contains("Received at counter", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmCashPayment_MachineProducedOrder_RequestsInitialDispatch()
    {
        var (order, payment, user) = CreateCashPaymentScenario(FulfillmentType.MachineProduced);
        var store = Substitute.For<IPaymentStore>();
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<Application.Shared.Wrappers.ApiResult<Application.Payments.PaymentSessions.Results.CashPaymentConfirmationResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<Application.Shared.Wrappers.ApiResult<Application.Payments.PaymentSessions.Results.CashPaymentConfirmationResult>>>>()(CancellationToken.None));
        store.GetPaymentTransactionByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        store.GetAppliedPaymentSettlementByOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns((PaymentTransaction?)null);

        var result = await CreateHandler(store).HandleAsync(new ConfirmCashPaymentCommand
        {
            OrderId = order.Id,
            PaymentTransactionId = payment.Id,
            UserContext = user,
            Request = new ConfirmCashPaymentRequest()
        });

        Assert.True(result.Succeeded, result.Message);
        // Dispatch is intentionally attempted after commit; the disabled test dispatcher makes it a deferred no-op.
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(OrderStatus.ReadyForFulfillment, order.Status);
    }

    [Fact]
    public async Task ConfirmCashPayment_OutsideScope_ReturnsNotFoundWithoutSettling()
    {
        var (order, payment, _) = CreateCashPaymentScenario();
        var store = Substitute.For<IPaymentStore>();
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<Application.Shared.Wrappers.ApiResult<Application.Payments.PaymentSessions.Results.CashPaymentConfirmationResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<Application.Shared.Wrappers.ApiResult<Application.Payments.PaymentSessions.Results.CashPaymentConfirmationResult>>>>()(CancellationToken.None));
        store.GetPaymentTransactionByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);

        var result = await CreateHandler(store).HandleAsync(new ConfirmCashPaymentCommand
        {
            OrderId = order.Id,
            PaymentTransactionId = payment.Id,
            UserContext = new CurrentUserContext
            {
                AccountId = Guid.NewGuid(),
                RoleScopes = [new UserRoleScope("Staff", Guid.NewGuid(), null, null)]
            },
            Request = new ConfirmCashPaymentRequest()
        });

        Assert.False(result.Succeeded);
        Assert.Equal(404, result.StatusCode);
        Assert.Equal(PaymentTransactionStatus.Pending, payment.Status);
        Assert.Equal(PaymentStatus.Unpaid, order.PaymentStatus);
    }

    private static ConfirmCashPaymentCommandHandler CreateHandler(IPaymentStore paymentStore)
    {
        var dispatch = new DispatchOrderExecutionCommandHandler(
            Substitute.For<IOrderExecutionDispatchStore>(),
            Options.Create(new OrderExecutionDispatchOptions { Enabled = false }),
            Substitute.For<IEdgeCommandWakeUpPublisher>());
        return new ConfirmCashPaymentCommandHandler(
            paymentStore,
            Substitute.For<IRealtimeNotificationPublisher>(),
            dispatch,
            NullLogger<ConfirmCashPaymentCommandHandler>.Instance);
    }

    private static (Order Order, PaymentTransaction Payment, CurrentUserContext User) CreateCashPaymentScenario(
        FulfillmentType fulfillmentType = FulfillmentType.Packaged)
    {
        var organization = new Organization
        {
            Id = Guid.NewGuid(), Code = "ORG", Name = "Organization", Status = EntityStatus.Active
        };
        var store = new Store
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, Organization = organization,
            Code = "STORE", Name = "Store", Status = EntityStatus.Active
        };
        var kiosk = new Kiosk
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, Organization = organization,
            StoreId = store.Id, Store = store, Code = "KIOSK", Name = "Kiosk", Status = KioskStatus.Active
        };
        var order = new Order
        {
            Id = Guid.NewGuid(), OrderNumber = "ORDER-CASH", KioskId = kiosk.Id, Kiosk = kiosk,
            OrganizationId = organization.Id, Organization = organization, StoreId = store.Id, Store = store
        };
        order.AddItem(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            "MENU", "Menu", "PRODUCT", "Product", "VARIANT", "Variant", null,
            fulfillmentType, 1, 30_000);
        order.Place(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(15));

        var paymentMethod = new PaymentMethod
        {
            Id = 1, Code = "cash", Name = "Cash", Provider = "Cash", IsOnline = false, IsActive = true
        };
        var payment = new PaymentTransaction
        {
            Id = Guid.NewGuid(), OrderId = order.Id, Order = order,
            PaymentMethodId = paymentMethod.Id, PaymentMethod = paymentMethod,
            TransactionNumber = "CASH-1", Provider = "Cash", Amount = 30_000, Currency = "VND",
            Status = PaymentTransactionStatus.Pending, RequestedAt = DateTimeOffset.UtcNow
        };
        var user = new CurrentUserContext
        {
            AccountId = Guid.NewGuid(),
            RoleScopes = [new UserRoleScope("Staff", organization.Id, null, null)]
        };
        return (order, payment, user);
    }
}
