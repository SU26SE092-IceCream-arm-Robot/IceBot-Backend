using Application.Payments.Abstractions;
using Application.Devices.Telemetry;
using Application.Orders.Admission;
using Application.Payments.PaymentSessions.Commands;
using Application.Payments.PaymentSessions.Requests;
using Application.Payments.PaymentSessions.Results;
using Application.Payments.Providers;
using Application.Shared.Wrappers;
using Domain.Orders.Entities;
using Domain.Payments.Entities;
using Domain.Payments.Enums;
using Domain.Catalog.Enums;
using Domain.Common.Enums;
using Domain.Devices.Connectivity;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using Application.SalesCatalog.Admission;
using Application.SalesCatalog.Admission.Abstractions;
using Application.SalesCatalog.Admission.Services;
using Application.Tenants.Kiosks.Rules;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace IceBot.UnitTests.Payments;

public sealed class CreatePaymentSessionCommandHandlerTests
{
    [Fact]
    public async Task ExistingFailedTransactionWithoutCheckoutUrl_DoesNotReturnSuccessfulSession()
    {
        var order = new Order { Id = Guid.NewGuid() };
        var paymentMethod = new PaymentMethod
        {
            Id = 1,
            Code = "payos",
            Name = "PayOS",
            Provider = "payos"
        };
        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            PaymentMethodId = paymentMethod.Id,
            PaymentMethod = paymentMethod,
            Provider = "payos",
            Amount = 30_000,
            Currency = "VND",
            Status = PaymentTransactionStatus.Failed,
            CheckoutUrl = null
        };
        var store = Substitute.For<IPaymentStore>();
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<ApiResult<PaymentSessionResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<PaymentSessionResult>>>>()(CancellationToken.None));
        store.GetOrderByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        store.GetPaymentTransactionByIdempotencyKeyAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(transaction);
        var gateway = Substitute.For<IPaymentGateway>();
        gateway.ProviderCode.Returns("payos");
        var handler = CreateHandler(store, gateway);

        var result = await handler.HandleAsync(new CreatePaymentSessionCommand
        {
            OrderId = order.Id,
            IdempotencyKey = "failed-session",
            Request = new CreatePaymentSessionRequest
            {
                PaymentMethodCode = "payos",
                ExpectedAmount = 30_000,
                ExpectedCurrency = "VND"
            }
        });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("new idempotency key", result.Message, StringComparison.OrdinalIgnoreCase);
        await gateway.DidNotReceive().CreatePaymentSessionAsync(
            Arg.Any<PaymentTransaction>(), Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistingPendingTransactionWithQrOnly_IsReturnedAsSuccessfulSession()
    {
        var order = new Order { Id = Guid.NewGuid() };
        var paymentMethod = new PaymentMethod
        {
            Id = 1,
            Code = "payos",
            Name = "PayOS",
            Provider = "payos"
        };
        var transaction = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Order = order,
            PaymentMethodId = paymentMethod.Id,
            PaymentMethod = paymentMethod,
            Provider = "payos",
            Amount = 30_000,
            Currency = "VND",
            Status = PaymentTransactionStatus.Pending,
            QrCodePayload = "qr-only"
        };
        var store = Substitute.For<IPaymentStore>();
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<ApiResult<PaymentSessionResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<PaymentSessionResult>>>>()(CancellationToken.None));
        store.GetOrderByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        store.GetPaymentTransactionByIdempotencyKeyAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(transaction);
        var gateway = Substitute.For<IPaymentGateway>();
        gateway.ProviderCode.Returns("payos");
        var handler = CreateHandler(store, gateway);

        var result = await handler.HandleAsync(new CreatePaymentSessionCommand
        {
            OrderId = order.Id,
            IdempotencyKey = "qr-session",
            Request = new CreatePaymentSessionRequest
            {
                PaymentMethodCode = "payos",
                ExpectedAmount = 30_000,
                ExpectedCurrency = "VND"
            }
        });

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal("qr-only", result.Data!.QrCodePayload);
        await gateway.DidNotReceive().CreatePaymentSessionAsync(
            Arg.Any<PaymentTransaction>(), Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnknownProviderCreateOutcome_RemainsPendingForReadSideReconciliation()
    {
        var scenario = ProviderCreateScenario.Create();
        scenario.Gateway.CreatePaymentSessionAsync(
                Arg.Any<PaymentTransaction>(), scenario.Order, Arg.Any<CancellationToken>())
            .Returns<Task<ProviderPaymentSession>>(_ => throw new ProviderPaymentSessionCreationException(
                "response lost",
                outcomeUnknown: true));

        var result = await scenario.Handler.HandleAsync(CreateCommand(scenario.Order.Id));

        Assert.False(result.Succeeded);
        Assert.NotNull(scenario.Payment);
        Assert.Equal(PaymentTransactionStatus.Pending, scenario.Payment.Status);
        Assert.Equal("PROVIDER_SESSION_CREATE_OUTCOME_UNKNOWN", scenario.Payment.LastErrorCode);
        Assert.Equal(1, scenario.Payment.RetryCount);
        Assert.NotNull(scenario.Payment.NextRetryAt);
        await scenario.Store.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task KnownProviderCreateRejection_MarksTransactionFailed()
    {
        var scenario = ProviderCreateScenario.Create();
        scenario.Gateway.CreatePaymentSessionAsync(
                Arg.Any<PaymentTransaction>(), scenario.Order, Arg.Any<CancellationToken>())
            .Returns<Task<ProviderPaymentSession>>(_ => throw new ProviderPaymentSessionCreationException(
                "request rejected",
                outcomeUnknown: false));

        var result = await scenario.Handler.HandleAsync(CreateCommand(scenario.Order.Id));

        Assert.False(result.Succeeded);
        Assert.NotNull(scenario.Payment);
        Assert.Equal(PaymentTransactionStatus.Failed, scenario.Payment.Status);
        Assert.Equal("PROVIDER_SESSION_CREATE_REJECTED", scenario.Payment.FailureCode);
    }

    [Fact]
    public async Task ExpiredOrderPaymentWindow_DoesNotCreatePaymentTransactionOrCallProvider()
    {
        var placedAt = DateTimeOffset.UtcNow.AddMinutes(-20);
        var scenario = ProviderCreateScenario.Create(placedAt, placedAt.AddMinutes(15));

        var result = await scenario.Handler.HandleAsync(CreateCommand(scenario.Order.Id));

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("payment window has expired", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(scenario.Payment);
        await scenario.Gateway.DidNotReceive().CreatePaymentSessionAsync(
            Arg.Any<PaymentTransaction>(), Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StorePausedAfterPlacement_DoesNotRevokeExistingOrderPaymentWindow()
    {
        var placedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var scenario = ProviderCreateScenario.Create(placedAt, placedAt.AddMinutes(15));
        scenario.Order.Kiosk.Store.PauseSales(
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            "Stop accepting new orders",
            null);
        scenario.Gateway.CreatePaymentSessionAsync(
                Arg.Any<PaymentTransaction>(), scenario.Order, Arg.Any<CancellationToken>())
            .Returns(new ProviderPaymentSession
            {
                CheckoutUrl = "https://payments.example/checkout",
                ExpiresAt = scenario.Order.PaymentDeadlineAt
            });

        var result = await scenario.Handler.HandleAsync(CreateCommand(scenario.Order.Id));

        Assert.True(result.Succeeded, result.Message);
        await scenario.Gateway.Received(1).CreatePaymentSessionAsync(
            Arg.Any<PaymentTransaction>(), scenario.Order, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IdempotentReplayAfterOrderPaymentDeadline_DoesNotReturnStaleInstructions()
    {
        var placedAt = DateTimeOffset.UtcNow.AddMinutes(-20);
        var scenario = ProviderCreateScenario.Create(placedAt, placedAt.AddMinutes(15));
        var paymentMethod = new PaymentMethod
        {
            Id = 1,
            Code = "payos",
            Name = "PayOS",
            Provider = "PayOS",
            IsActive = true
        };
        var existing = new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            OrderId = scenario.Order.Id,
            Order = scenario.Order,
            PaymentMethodId = paymentMethod.Id,
            PaymentMethod = paymentMethod,
            Provider = "PayOS",
            Amount = 30_000,
            Currency = "VND",
            Status = PaymentTransactionStatus.Pending,
            CheckoutUrl = "https://payments.example/stale"
        };
        scenario.Store.GetPaymentTransactionByIdempotencyKeyAsync(
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await scenario.Handler.HandleAsync(CreateCommand(scenario.Order.Id));

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("payment window has expired", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CashPayment_CreatesPendingStaffConfirmationWithoutCallingGateway()
    {
        var scenario = ProviderCreateScenario.Create();
        var cashMethod = new PaymentMethod
        {
            Id = 2,
            Code = "cash",
            Name = "Cash",
            Provider = "Cash",
            IsOnline = false,
            IsActive = true
        };
        scenario.Store.GetPaymentMethodByCodeAsync("cash", Arg.Any<CancellationToken>()).Returns(cashMethod);
        scenario.Store.AddPaymentTransactionAsync(
                Arg.Do<PaymentTransaction>(payment =>
                {
                    payment.Order = scenario.Order;
                    payment.PaymentMethod = cashMethod;
                    scenario.Payment = payment;
                }),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await scenario.Handler.HandleAsync(new CreatePaymentSessionCommand
        {
            OrderId = scenario.Order.Id,
            IdempotencyKey = "cash-payment",
            Request = new CreatePaymentSessionRequest
            {
                PaymentMethodCode = "cash",
                ExpectedAmount = 30_000,
                ExpectedCurrency = "VND"
            }
        });

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(PaymentTransactionStatus.Pending, result.Data!.Status);
        Assert.NotNull(scenario.Payment);
        Assert.Equal("Cash", scenario.Payment!.Provider);
        await scenario.Gateway.DidNotReceive().CreatePaymentSessionAsync(
            Arg.Any<PaymentTransaction>(), Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    private sealed class ProviderCreateScenario
    {
        public required CreatePaymentSessionCommandHandler Handler { get; init; }
        public required IPaymentStore Store { get; init; }
        public required IPaymentGateway Gateway { get; init; }
        public required Order Order { get; init; }
        public PaymentTransaction? Payment { get; set; }

        public static ProviderCreateScenario Create(
            DateTimeOffset? placedAtOverride = null,
            DateTimeOffset? paymentDeadlineAtOverride = null)
        {
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Code = "ORG",
                Name = "Organization",
                Status = EntityStatus.Active
            };
            var storeEntity = new Store
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Organization = organization,
                Code = "STORE",
                Name = "Store",
                Status = EntityStatus.Active
            };
            var kiosk = new Kiosk
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization.Id,
                Organization = organization,
                StoreId = storeEntity.Id,
                Store = storeEntity,
                Code = "KIOSK",
                Name = "Kiosk",
                Status = KioskStatus.Active
            };
            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORDER-1",
                KioskId = kiosk.Id,
                Kiosk = kiosk,
                OrganizationId = organization.Id,
                Organization = organization,
                StoreId = storeEntity.Id,
                Store = storeEntity
            };
            order.AddItem(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
                "MENU", "Menu", "PRODUCT", "Product", "VARIANT", "Variant", null,
                FulfillmentType.Packaged, 1, 30_000);
            var placedAt = placedAtOverride ?? DateTimeOffset.UtcNow;
            order.Place(placedAt, paymentDeadlineAtOverride ?? placedAt.AddMinutes(15));
            var connectivity = KioskConnectivityProjection.Create(kiosk.Id, DateTimeOffset.UtcNow);
            connectivity.Observe(
                KioskConnectivityStatus.Online,
                Guid.NewGuid(),
                1,
                DateTimeOffset.UtcNow);
            var paymentMethod = new PaymentMethod
            {
                Id = 1,
                Code = "payos",
                Name = "PayOS",
                Provider = "PayOS",
                IsActive = true
            };
            var paymentStore = Substitute.For<IPaymentStore>();
            var gateway = Substitute.For<IPaymentGateway>();
            gateway.ProviderCode.Returns("PayOS");
            gateway.CreateProviderOrderCode(Arg.Any<Guid>()).Returns("1234567890123");
            paymentStore.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<ApiResult<PaymentSessionResult>>>>(),
                Arg.Any<CancellationToken>())
                .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<PaymentSessionResult>>>>()(
                    CancellationToken.None));
            paymentStore.GetOrderByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
            paymentStore.GetKioskConnectivityAsync(kiosk.Id, Arg.Any<CancellationToken>()).Returns(connectivity);
            paymentStore.GetPaymentMethodByCodeAsync("payos", Arg.Any<CancellationToken>()).Returns(paymentMethod);

            var scenario = new ProviderCreateScenario
            {
                Handler = CreateHandler(paymentStore, gateway),
                Store = paymentStore,
                Gateway = gateway,
                Order = order
            };
            paymentStore.AddPaymentTransactionAsync(
                    Arg.Do<PaymentTransaction>(payment =>
                    {
                        payment.Order = order;
                        payment.PaymentMethod = paymentMethod;
                        scenario.Payment = payment;
                    }),
                    Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            paymentStore.GetPaymentTransactionByIdAsync(
                    Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns(_ => scenario.Payment);
            return scenario;
        }
    }

    private static CreatePaymentSessionCommand CreateCommand(Guid orderId) => new()
    {
        OrderId = orderId,
        IdempotencyKey = "provider-create",
        Request = new CreatePaymentSessionRequest
        {
            PaymentMethodCode = "payos",
            ExpectedAmount = 30_000,
            ExpectedCurrency = "VND"
        }
    };

    private static CreatePaymentSessionCommandHandler CreateHandler(
        IPaymentStore paymentStore,
        IPaymentGateway paymentGateway)
    {
        var itemAdmission = Substitute.For<IMenuItemOperationalAdmissionEvaluator>();
        itemAdmission.EvaluateAsync(
                Arg.Any<Kiosk>(),
                Arg.Any<Guid>(),
                Arg.Any<int>(),
                Arg.Any<IReadOnlyCollection<Application.Inventory.Abstractions.InventoryIngredientRequirementInput>?>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(call => new MenuItemOperationalDecision(
                call.ArgAt<Guid>(1),
                true,
                [],
                [],
                new HashSet<string>()));
        var sellabilityGuard = new OrderPaymentSellabilityGuard(itemAdmission);
        return new CreatePaymentSessionCommandHandler(
            paymentStore,
            paymentGateway,
            sellabilityGuard,
            new KioskSalesAdmissionEvaluator(
                Substitute.For<IOperationalAdmissionReadStore>(),
                Options.Create(new KioskSalesAdmissionOptions { RequireConnectivity = false }),
                Options.Create(new EdgeTelemetryIngestionOptions())));
    }
}
