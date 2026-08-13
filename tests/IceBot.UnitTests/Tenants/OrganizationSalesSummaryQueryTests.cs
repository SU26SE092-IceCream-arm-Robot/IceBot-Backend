using Application.Identity.Tokens.Claims;
using Application.Tenants.Organizations.Abstractions;
using Application.Tenants.Organizations.Queries;
using Application.Tenants.Organizations.ReadModels;
using NSubstitute;

namespace IceBot.UnitTests.Tenants;

public sealed class OrganizationSalesSummaryQueryTests
{
    [Fact]
    public async Task NonSystemAdmin_IsForbiddenBeforeStoreAccess()
    {
        var store = Substitute.For<IOrganizationSalesSummaryStore>();
        var handler = new ListOrganizationSalesSummariesQueryHandler(store);

        var result = await handler.HandleAsync(CreateQuery(isSystemAdmin: false));

        Assert.False(result.Succeeded);
        Assert.Equal(403, result.StatusCode);
        await store.DidNotReceive().CountAsync(Arg.Any<OrganizationSalesSummaryReadRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RangeLongerThan366Days_IsRejectedBeforeStoreAccess()
    {
        var store = Substitute.For<IOrganizationSalesSummaryStore>();
        var handler = new ListOrganizationSalesSummariesQueryHandler(store);
        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var result = await handler.HandleAsync(CreateQuery(isSystemAdmin: true, from: from, to: from.AddDays(367)));

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("366", result.Message);
        await store.DidNotReceive().CountAsync(Arg.Any<OrganizationSalesSummaryReadRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NonUtcPeriod_IsRejectedBeforeStoreAccess()
    {
        var store = Substitute.For<IOrganizationSalesSummaryStore>();
        var handler = new ListOrganizationSalesSummariesQueryHandler(store);
        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(7));

        var result = await handler.HandleAsync(CreateQuery(isSystemAdmin: true, from: from, to: from.AddDays(1)));

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("UTC", result.Message);
        await store.DidNotReceive().CountAsync(Arg.Any<OrganizationSalesSummaryReadRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidSystemAdminRequest_ReturnsSeparateGrossRefundAndNetAmounts()
    {
        var store = Substitute.For<IOrganizationSalesSummaryStore>();
        var organizationId = Guid.NewGuid();
        store.CountAsync(Arg.Any<OrganizationSalesSummaryReadRequest>(), Arg.Any<CancellationToken>()).Returns(1);
        store.ListAsync(Arg.Any<OrganizationSalesSummaryReadRequest>(), Arg.Any<CancellationToken>()).Returns(
        [
            new OrganizationSalesSummaryReadModel(
                organizationId, "ORG-A", "Organization A", "Archived", "VND", 1, 60_000m, 90_000m)
        ]);
        var handler = new ListOrganizationSalesSummariesQueryHandler(store);

        var result = await handler.HandleAsync(CreateQuery(isSystemAdmin: true));
        var summary = Assert.Single(result.Data!);

        Assert.True(result.Succeeded);
        Assert.Equal(1, summary.PaidOrderCount);
        Assert.Equal(60_000m, summary.GrossCollectedAmount);
        Assert.Equal(90_000m, summary.ProcessedRefundAmount);
        Assert.Equal(-30_000m, summary.NetCollectedAmount);
        Assert.Equal("Archived", summary.OrganizationStatus);
    }

    private static ListOrganizationSalesSummariesQuery CreateQuery(
        bool isSystemAdmin,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null) =>
        new()
        {
            UserContext = new CurrentUserContext { AccountId = Guid.NewGuid(), IsSystemAdmin = isSystemAdmin },
            From = from ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            To = to ?? new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero)
        };
}
