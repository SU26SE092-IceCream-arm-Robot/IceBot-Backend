using Application.Tenants.Organizations.ReadModels;
using System.Net;
using System.Text.Json;
using Domain.Common.Enums;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.Payments.Entities;
using Domain.Payments.Enums;
using Domain.Tenants.Entities;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Tenants.Persistence;

namespace IceBot.IntegrationTests.Tenants;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class OrganizationSalesSummaryIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task HistoricalPrimarySettlementsAndProcessedRefunds_AreAggregatedByTheirOwnPeriods()
    {
        var periodStart = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var periodEnd = periodStart.AddMonths(1);
        var organizationId = await SeedAsync(periodStart);

        await using var dbContext = fixture.CreateDbContext();
        var store = new OrganizationSalesSummaryStore(dbContext);
        var summaries = await store.ListAsync(new OrganizationSalesSummaryReadRequest(
            periodStart, periodEnd, organizationId, null, 1, 20));
        var total = await store.CountAsync(new OrganizationSalesSummaryReadRequest(
            periodStart, periodEnd, organizationId, null, 1, 20));

        var summary = Assert.Single(summaries);
        Assert.Equal(1, total);
        Assert.Equal("Archived sales organization", summary.OrganizationName);
        Assert.Equal("Archived", summary.OrganizationStatus);
        Assert.Equal("VND", summary.Currency);
        Assert.Equal(1, summary.PaidOrderCount);
        Assert.Equal(60_000m, summary.GrossCollectedAmount);
        Assert.Equal(30_000m, summary.ProcessedRefundAmount);
    }

    [IntegrationFact]
    public async Task Api_ExposesAggregateRowsOnlyToSystemAdmin()
    {
        var periodStart = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var organizationId = await SeedAsync(periodStart);
        var storage = fixture.CreateObjectStorage(autoCreateBucket: true);
        var query = $"/api/v1/management/organizations/sales-summaries?from={Uri.EscapeDataString(periodStart.ToString("O"))}&to={Uri.EscapeDataString(periodStart.AddMonths(1).ToString("O"))}&organizationId={organizationId:D}";

        await using (var systemAdminFactory = new PackageApiWebApplicationFactory(
            fixture, storage, Guid.NewGuid(), "SystemAdmin"))
        using (var systemAdminClient = systemAdminFactory.CreateAuthenticatedClient())
        using (var response = await systemAdminClient.GetAsync(query))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var row = payload.RootElement.GetProperty("data")[0];
            Assert.Equal(organizationId, row.GetProperty("organizationId").GetGuid());
            Assert.False(row.TryGetProperty("orderId", out _));
            Assert.False(row.TryGetProperty("providerTransactionId", out _));
            Assert.False(row.TryGetProperty("paidAt", out _));
        }

        await using var orgAdminFactory = new PackageApiWebApplicationFactory(
            fixture, storage, Guid.NewGuid(), "OrgAdmin", [$"OrgAdmin|{organizationId:D}|*|*"]);
        using var orgAdminClient = orgAdminFactory.CreateAuthenticatedClient();
        using var forbiddenResponse = await orgAdminClient.GetAsync(query);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
    }

    private async Task<Guid> SeedAsync(DateTimeOffset periodStart)
    {
        await using var dbContext = fixture.CreateDbContext();
        var organization = new Organization
        {
            Code = $"SALES-{Guid.NewGuid():N}",
            Name = "Archived sales organization",
            Status = EntityStatus.Archived
        };
        var store = new Store
        {
            OrganizationId = organization.Id,
            Code = $"STORE-{Guid.NewGuid():N}",
            Name = "Sales store",
            Status = EntityStatus.Active
        };
        var kiosk = new Kiosk
        {
            OrganizationId = organization.Id,
            StoreId = store.Id,
            Code = $"KIOSK-{Guid.NewGuid():N}",
            Name = "Sales kiosk"
        };
        var paymentMethod = new PaymentMethod
        {
            Code = $"PAYMENT-{Guid.NewGuid():N}",
            Name = "Sales payment method",
            Provider = "Test",
            IsActive = true
        };
        var refundedOrder = CreateOrder(organization.Id, store.Id, kiosk.Id);
        var currentPeriodOrder = CreateOrder(organization.Id, store.Id, kiosk.Id);
        var unassignedOrder = CreateOrder(organization.Id, store.Id, kiosk.Id);

        dbContext.AddRange(organization, store, kiosk, paymentMethod, refundedOrder, currentPeriodOrder, unassignedOrder);
        await dbContext.SaveChangesAsync();

        var primaryPaidBeforePeriod = CreatePaidTransaction(
            refundedOrder, paymentMethod, 100_000m, periodStart.AddDays(-10), primary: true);
        var primaryPaidInPeriod = CreatePaidTransaction(
            currentPeriodOrder, paymentMethod, 60_000m, periodStart.AddDays(10), primary: true);
        var unassignedPaidInPeriod = CreatePaidTransaction(
            unassignedOrder, paymentMethod, 45_000m, periodStart.AddDays(12), primary: false);
        dbContext.PaymentTransactions.AddRange(primaryPaidBeforePeriod, primaryPaidInPeriod, unassignedPaidInPeriod);
        await dbContext.SaveChangesAsync();

        var primaryRefund = new Refund
        {
            PaymentTransactionId = primaryPaidBeforePeriod.Id,
            Amount = 30_000m,
            Currency = "VND",
            RefundNumber = $"REFUND-{Guid.NewGuid():N}",
            Reason = "Test partial refund",
            Status = RefundStatus.Processed,
            RequestedAt = periodStart.AddDays(8),
            ProcessedAt = periodStart.AddDays(9)
        };
        var unassignedRefund = new Refund
        {
            PaymentTransactionId = unassignedPaidInPeriod.Id,
            Amount = 45_000m,
            Currency = "VND",
            RefundNumber = $"REFUND-{Guid.NewGuid():N}",
            Reason = "Unassigned payment must not be aggregated",
            Status = RefundStatus.Processed,
            RequestedAt = periodStart.AddDays(13),
            ProcessedAt = periodStart.AddDays(14)
        };
        dbContext.Refunds.AddRange(primaryRefund, unassignedRefund);
        await dbContext.SaveChangesAsync();
        return organization.Id;
    }

    private static Order CreateOrder(Guid organizationId, Guid storeId, Guid kioskId) => new()
    {
        OrganizationId = organizationId,
        StoreId = storeId,
        KioskId = kioskId,
        Channel = OrderChannel.Admin,
        OrderNumber = $"SALES-ORDER-{Guid.NewGuid():N}"
    };

    private static PaymentTransaction CreatePaidTransaction(
        Order order,
        PaymentMethod paymentMethod,
        decimal amount,
        DateTimeOffset paidAt,
        bool primary)
    {
        var transaction = new PaymentTransaction
        {
            OrderId = order.Id,
            PaymentMethodId = paymentMethod.Id,
            TransactionNumber = $"SALES-TX-{Guid.NewGuid():N}",
            Provider = "Test",
            Amount = amount,
            PaidAmount = amount,
            Currency = "VND",
            RequestedAt = paidAt.AddMinutes(-1)
        };
        transaction.MarkPaid($"provider-{Guid.NewGuid():N}", paidAt);
        if (primary)
        {
            transaction.AssignPrimarySettlement();
        }

        return transaction;
    }
}
