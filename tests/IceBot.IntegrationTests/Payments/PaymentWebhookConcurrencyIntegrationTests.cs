using Application.EdgeIntegration.Dispatch;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.Payments.Abstractions;
using Application.Payments.PaymentSessions.Commands;
using Application.Payments.PaymentSessions.Requests;
using Application.Payments.Providers;
using Application.Orders.PlaceOrder.Commands;
using Application.Orders.PlaceOrder.Requests;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.Payments.Entities;
using Domain.Payments.Enums;
using Domain.SalesCatalog.Entities;
using Domain.Tenants.Entities;
using Domain.Common.Enums;
using Domain.Tenants.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.EdgeIntegration.Persistence;
using Infrastructure.Payments.Persistence;
using Infrastructure.Orders.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IceBot.IntegrationTests.Payments;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class PaymentWebhookConcurrencyIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task ConcurrentDistinctPaidEvents_AreSerializedPerProviderPayment()
    {
        var seed = await SeedAsync();
        var first = HandleAsync(seed, "event:first");
        var second = HandleAsync(seed, "event:second");

        var results = await Task.WhenAll(first, second);

        Assert.All(results, result => Assert.True(result.Succeeded, result.Message));
        await using var assertionContext = fixture.CreateDbContext();
        var payment = await assertionContext.PaymentTransactions.SingleAsync(x => x.Id == seed.PaymentId);
        var order = await assertionContext.Orders.SingleAsync(x => x.Id == seed.OrderId);
        Assert.Equal(PaymentTransactionStatus.Paid, payment.Status);
        Assert.Equal(seed.Amount, order.PaidAmount);
        Assert.Equal(2, await assertionContext.PaymentCallbacks.CountAsync(x => x.PaymentTransactionId == seed.PaymentId));
    }

    [IntegrationFact]
    public async Task TwoProviderConfirmedSessions_ForSameOrder_RequireManualRefundReview()
    {
        var first = await SeedAsync();
        var firstResult = await HandleAsync(first, "event:first-session-paid");
        Assert.True(firstResult.Succeeded, firstResult.Message);

        Seed second;
        await using (var mutation = fixture.CreateDbContext())
        {
            var firstPayment = await mutation.PaymentTransactions.AsNoTracking()
                .SingleAsync(payment => payment.Id == first.PaymentId);
            var secondPayment = new PaymentTransaction
            {
                OrderId = first.OrderId,
                PaymentMethodId = firstPayment.PaymentMethodId,
                TransactionNumber = $"TX-{Guid.NewGuid():N}",
                Provider = firstPayment.Provider,
                ProviderOrderCode = Random.Shared
                    .NextInt64(1_000_000_000_000, 9_999_999_999_999)
                    .ToString(),
                Amount = first.Amount,
                Currency = firstPayment.Currency,
                Status = PaymentTransactionStatus.Pending,
                RequestedAt = DateTimeOffset.UtcNow
            };
            mutation.PaymentTransactions.Add(secondPayment);
            await mutation.SaveChangesAsync();
            second = new Seed(
                first.OrganizationId,
                first.OrderId,
                secondPayment.Id,
                secondPayment.ProviderOrderCode,
                first.Amount);
        }

        var secondResult = await HandleAsync(second, "event:second-session-paid");

        Assert.True(secondResult.Succeeded, secondResult.Message);
        await using var assertion = fixture.CreateDbContext();
        var order = await assertion.Orders.AsNoTracking()
            .SingleAsync(candidate => candidate.Id == first.OrderId);
        Assert.Equal(first.Amount, order.PaidAmount);
        Assert.Equal(OrderStatus.RefundRequired, order.Status);
        Assert.Equal(2, await assertion.PaymentTransactions.CountAsync(payment =>
            payment.OrderId == first.OrderId && payment.Status == PaymentTransactionStatus.Paid));
        Assert.Equal(1, await assertion.PaymentTransactions.CountAsync(payment =>
            payment.OrderId == first.OrderId &&
            payment.SettlementDisposition == PaymentSettlementDisposition.Primary));
        Assert.Equal(1, await assertion.PaymentTransactions.CountAsync(payment =>
            payment.OrderId == first.OrderId &&
            payment.SettlementDisposition == PaymentSettlementDisposition.DuplicateRefundRequired));
    }

    [IntegrationFact]
    public async Task ConcurrentCancellationAndPaidWebhook_PreservesPaidEvidenceAndSafeOrderState()
    {
        var seed = await SeedAsync();
        var webhook = HandleAsync(seed, "event:cancel-race");
        async Task<Application.Shared.Wrappers.ApiResult<Application.Orders.PlaceOrder.Results.OrderResult>> CancelAsync()
        {
            await using var db = fixture.CreateDbContext();
            return await new CancelPendingOrderCommandHandler(
                new OrderStore(db),
                new NoOpRealtimeNotificationPublisher())
                .HandleAsync(new CancelPendingOrderCommand
                {
                    OrderId = seed.OrderId,
                    Request = new CancelPendingOrderRequest { Reason = "Customer cancelled during payment." }
                });
        }

        var cancel = CancelAsync();
        var webhookResult = await webhook;
        await cancel;

        Assert.True(webhookResult.Succeeded, webhookResult.Message);
        await using var assertion = fixture.CreateDbContext();
        var payment = await assertion.PaymentTransactions.AsNoTracking()
            .SingleAsync(candidate => candidate.Id == seed.PaymentId);
        var order = await assertion.Orders.AsNoTracking()
            .SingleAsync(candidate => candidate.Id == seed.OrderId);
        Assert.Equal(PaymentTransactionStatus.Paid, payment.Status);
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(seed.Amount, order.PaidAmount);
        Assert.Contains(order.Status, new[] { OrderStatus.ReadyForFulfillment, OrderStatus.RefundRequired });
    }

    [IntegrationFact]
    public async Task InterventionQueue_IsTenantScopedAndReturnsIncompleteSession()
    {
        var seed = await SeedAsync();
        await using var mutationContext = fixture.CreateDbContext();
        var payment = await mutationContext.PaymentTransactions.SingleAsync(x => x.Id == seed.PaymentId);
        payment.LastErrorCode = "AWAITING_SIGNED_WEBHOOK";
        payment.LastErrorMessage = "Signed webhook has not arrived.";
        payment.RetryCount = payment.MaxRetries;
        payment.CheckoutUrl = "https://pay.test/expired-session";
        payment.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await mutationContext.SaveChangesAsync();

        await using var queryContext = fixture.CreateDbContext();
        var paymentStore = new PaymentStore(queryContext);
        var observedAt = DateTimeOffset.UtcNow;
        var allowed = await paymentStore.ListPaymentSessionInterventionsAsync(
            observedAt,
            null, null, null, null, null, false,
            new[] { seed.OrganizationId }, Array.Empty<Guid>(), Array.Empty<Guid>(), 1, 20);
        var denied = await paymentStore.ListPaymentSessionInterventionsAsync(
            observedAt,
            null, null, null, null, null, false,
            new[] { Guid.NewGuid() }, Array.Empty<Guid>(), Array.Empty<Guid>(), 1, 20);

        Assert.Contains(allowed, item => item.Id == seed.PaymentId);
        Assert.DoesNotContain(denied, item => item.Id == seed.PaymentId);
    }

    private async Task<Application.Shared.Wrappers.ApiResult<Application.Payments.PaymentSessions.Results.PaymentNotificationResult>>
        HandleAsync(Seed seed, string eventId)
    {
        await using var dbContext = fixture.CreateDbContext();
        var gateway = new FixedNotificationGateway(new ProviderPaymentNotification
        {
            Provider = "PayOS",
            ProviderEventId = eventId,
            ProviderOrderCode = seed.ProviderOrderCode,
            EventType = "PAID",
            ProviderStatus = "PAID",
            IsPaid = true,
            PaidAmount = seed.Amount,
            ProviderPaidAt = DateTimeOffset.UtcNow,
            RawPayloadJson = "{}"
        });
        var handler = new HandlePaymentProviderNotificationCommandHandler(
            new PaymentStore(dbContext),
            gateway,
            new NoOpRealtimeNotificationPublisher(),
            new DispatchOrderExecutionCommandHandler(
                new OrderExecutionDispatchStore(dbContext),
                Options.Create(new OrderExecutionDispatchOptions()),
                new NoOpEdgeCommandWakeUpPublisher()),
            NullLogger<HandlePaymentProviderNotificationCommandHandler>.Instance);
        return await handler.HandleAsync(new HandlePaymentProviderNotificationCommand
        {
            Request = new HandlePaymentProviderNotificationRequest { RawPayload = "{}" }
        });
    }

    private async Task<Seed> SeedAsync()
    {
        await using var dbContext = fixture.CreateDbContext();
        var organization = new Organization
        {
            Code = $"PAY-{Guid.NewGuid():N}",
            Name = "Payment concurrency organization",
            Status = EntityStatus.Active
        };
        var store = new Store
        {
            OrganizationId = organization.Id,
            Code = $"STORE-{Guid.NewGuid():N}",
            Name = "Payment concurrency store",
            Status = EntityStatus.Active
        };
        var kiosk = new Kiosk
        {
            OrganizationId = organization.Id,
            StoreId = store.Id,
            Code = $"KIOSK-{Guid.NewGuid():N}",
            Name = "Payment concurrency kiosk"
        };
        const decimal amount = 30_000;
        var order = new Order
        {
            OrganizationId = organization.Id,
            StoreId = store.Id,
            KioskId = kiosk.Id,
            OrderNumber = $"ORDER-{Guid.NewGuid():N}"
        };
        order.SetCurrency("VND");
        var product = new Product
        {
            OrganizationId = organization.Id,
            ScopeType = TenantScopeType.Organization,
            Code = $"PRODUCT-{Guid.NewGuid():N}",
            Name = "Payment product",
            ProductType = "Packaged",
            BasePrice = amount,
            Currency = "VND"
        };
        var variant = new ProductVariant
        {
            ProductId = product.Id,
            Code = "DEFAULT",
            Name = "Default",
            FulfillmentType = FulfillmentType.Packaged,
            BasePrice = amount,
            Currency = "VND"
        };
        var menu = new Menu
        {
            OrganizationId = organization.Id,
            StoreId = store.Id,
            KioskId = kiosk.Id,
            ScopeType = TenantScopeType.Kiosk,
            Code = $"MENU-{Guid.NewGuid():N}",
            Name = "Payment menu"
        };
        var menuItem = new MenuItem
        {
            MenuId = menu.Id,
            ProductId = product.Id,
            ProductVariantId = variant.Id,
            Code = $"ITEM-{Guid.NewGuid():N}",
            DisplayName = "Payment item",
            Price = amount,
            Currency = "VND"
        };
        product.ProductVariants.Add(variant);
        menu.MenuItems.Add(menuItem);
        order.AddItem(
            menuItem.Id, product.Id, variant.Id, null,
            menuItem.Code, menuItem.DisplayName, product.Code, product.Name,
            variant.Code, variant.Name, null, FulfillmentType.Packaged, 1, amount);
        var placedAt = DateTimeOffset.UtcNow;
        order.Place(placedAt, placedAt.AddMinutes(15));
        var paymentMethod = new PaymentMethod
        {
            Code = $"payos-{Guid.NewGuid():N}",
            Name = "PayOS",
            Provider = "PayOS",
            IsActive = true
        };
        var providerOrderCode = Random.Shared.NextInt64(1_000_000_000_000, 9_999_999_999_999).ToString();
        var payment = new PaymentTransaction
        {
            OrderId = order.Id,
            Order = order,
            PaymentMethodId = paymentMethod.Id,
            PaymentMethod = paymentMethod,
            TransactionNumber = $"TX-{Guid.NewGuid():N}",
            Provider = "PayOS",
            ProviderOrderCode = providerOrderCode,
            Amount = amount,
            Currency = "VND",
            Status = PaymentTransactionStatus.Pending,
            RequestedAt = DateTimeOffset.UtcNow
        };
        dbContext.AddRange(organization, store, kiosk, product, menu, order, paymentMethod, payment);
        await dbContext.SaveChangesAsync();
        return new Seed(organization.Id, order.Id, payment.Id, providerOrderCode, amount);
    }

    private sealed record Seed(
        Guid OrganizationId,
        Guid OrderId,
        Guid PaymentId,
        string ProviderOrderCode,
        decimal Amount);

    private sealed class FixedNotificationGateway(ProviderPaymentNotification notification) : IPaymentGateway
    {
        public string ProviderCode => "PayOS";
        public string CreateProviderOrderCode(Guid paymentTransactionId) => throw new NotSupportedException();
        public Task<ProviderPaymentSession> CreatePaymentSessionAsync(
            PaymentTransaction paymentTransaction,
            Order order,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ProviderPaymentSession?> GetPaymentSessionAsync(
            string providerOrderCode,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ProviderPaymentNotification> ParseAndVerifyNotificationAsync(
            string rawPayload,
            string? signature,
            CancellationToken cancellationToken = default) => Task.FromResult(notification);
    }
}
