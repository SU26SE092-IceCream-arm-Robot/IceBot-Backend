using Application.Identity.Tokens.Claims;
using Application.Tenants.Abstractions;
using Application.Tenants.Stores.Commands;
using Application.Tenants.Stores.Requests;
using Domain.Common.Enums;
using Domain.Tenants.Entities;
using NSubstitute;

namespace IceBot.UnitTests.Tenants;

public sealed class StoreSalesPauseCommandTests
{
    [Fact]
    public async Task Update_RejectsTimeZoneChangeForOpeningHoursUntilSalesArePaused()
    {
        var store = new Store
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Name = "Central",
            TimeZone = "Asia/Bangkok",
            OpeningHoursJson = "[{\"dayOfWeek\":1,\"isClosed\":false,\"opensAt\":\"08:00:00\",\"closesAt\":\"22:00:00\"}]"
        };
        var storeStore = Substitute.For<IStoreStore>();
        storeStore.GetByIdAsync(store.Id, Arg.Any<CancellationToken>()).Returns(store);
        var handler = new UpdateStoreCommandHandler(storeStore);

        var result = await handler.HandleAsync(new UpdateStoreCommand
        {
            StoreId = store.Id,
            UserContext = new CurrentUserContext { IsSystemAdmin = true, AccountId = Guid.NewGuid() },
            Request = new UpdateStoreRequest
            {
                Name = store.Name,
                TimeZone = "Asia/Singapore",
                OpeningHours =
                [
                    new StoreOpeningHoursDayRequest
                    {
                        DayOfWeek = DayOfWeek.Monday,
                        OpensAt = new TimeOnly(8, 0),
                        ClosesAt = new TimeOnly(22, 0)
                    }
                ]
            }
        });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("Asia/Bangkok", store.TimeZone);
        await storeStore.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pause_UsesOrganizationScopedLookupAndPersistsActorAndReason()
    {
        var organizationId = Guid.NewGuid();
        var store = new Store
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Status = EntityStatus.Active
        };
        var actorId = Guid.NewGuid();
        var storeStore = Substitute.For<IStoreStore>();
        storeStore.GetByOrganizationAndIdAsync(
                organizationId, store.Id, Arg.Any<CancellationToken>())
            .Returns(store);
        var handler = new PauseStoreSalesCommandHandler(storeStore);

        var result = await handler.HandleAsync(new PauseStoreSalesCommand
        {
            OrganizationId = organizationId,
            StoreId = store.Id,
            UserContext = new CurrentUserContext { IsSystemAdmin = true, AccountId = actorId },
            Request = new PauseStoreSalesRequest { Reason = "Early maintenance" }
        });

        Assert.True(result.Succeeded, result.Message);
        Assert.True(store.IsSalesPausedAt(DateTimeOffset.UtcNow));
        Assert.Equal(actorId, store.SalesPausedByAccountId);
        Assert.Equal("Early maintenance", store.SalesPauseReason);
        await storeStore.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pause_WithStoreOutsideRouteOrganizationReturnsNotFoundWithoutMutation()
    {
        var requestedOrganizationId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var storeStore = Substitute.For<IStoreStore>();
        storeStore.GetByOrganizationAndIdAsync(
                requestedOrganizationId, storeId, Arg.Any<CancellationToken>())
            .Returns((Store?)null);
        var handler = new PauseStoreSalesCommandHandler(storeStore);

        var result = await handler.HandleAsync(new PauseStoreSalesCommand
        {
            OrganizationId = requestedOrganizationId,
            StoreId = storeId,
            UserContext = new CurrentUserContext { IsSystemAdmin = true, AccountId = Guid.NewGuid() },
            Request = new PauseStoreSalesRequest { Reason = "Maintenance" }
        });

        Assert.False(result.Succeeded);
        Assert.Equal(404, result.StatusCode);
        await storeStore.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
