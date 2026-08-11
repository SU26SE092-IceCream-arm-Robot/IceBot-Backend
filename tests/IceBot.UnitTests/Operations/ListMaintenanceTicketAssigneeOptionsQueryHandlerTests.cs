using Application.Identity.Tokens.Claims;
using Application.Operations.Abstractions;
using Application.Operations.MaintenanceTickets.Queries;
using Application.Operations.MaintenanceTickets.Results;
using NSubstitute;

namespace IceBot.UnitTests.Operations;

public sealed class ListMaintenanceTicketAssigneeOptionsQueryHandlerTests
{
    [Fact]
    public async Task ManagerInTicketScope_GetsOnlyScopedAssignableAccounts()
    {
        var organizationId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var kioskId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var store = Substitute.For<IMaintenanceTicketStore>();
        store.GetByIdAsync(ticketId, Arg.Any<CancellationToken>())
            .Returns(new Domain.Operations.Entities.MaintenanceTicket
            {
                OrganizationId = organizationId,
                StoreId = storeId,
                KioskId = kioskId
            });
        store.ListAssignableAccountsAsync(organizationId, storeId, kioskId, Arg.Any<CancellationToken>())
            .Returns([
                new MaintenanceAssigneeOptionResult
                {
                    AccountId = Guid.NewGuid(),
                    DisplayName = "Technician",
                    RoleCodes = ["Technician"]
                }
            ]);
        var handler = new ListMaintenanceTicketAssigneeOptionsQueryHandler(store);

        var result = await handler.HandleAsync(new ListMaintenanceTicketAssigneeOptionsQuery
        {
            TicketId = ticketId,
            UserContext = new CurrentUserContext
            {
                RoleScopes = [new UserRoleScope("Manager", organizationId, storeId, null)]
            }
        });

        Assert.True(result.Succeeded);
        Assert.Single(result.Data!);
        await store.Received(1).ListAssignableAccountsAsync(
            organizationId, storeId, kioskId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ManagerOutsideTicketScope_CannotEnumerateAssignees()
    {
        var store = Substitute.For<IMaintenanceTicketStore>();
        var kioskId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        store.GetByIdAsync(ticketId, Arg.Any<CancellationToken>())
            .Returns(new Domain.Operations.Entities.MaintenanceTicket
            {
                OrganizationId = Guid.NewGuid(),
                StoreId = Guid.NewGuid(),
                KioskId = kioskId
            });
        var handler = new ListMaintenanceTicketAssigneeOptionsQueryHandler(store);

        var result = await handler.HandleAsync(new ListMaintenanceTicketAssigneeOptionsQuery
        {
            TicketId = ticketId,
            UserContext = new CurrentUserContext
            {
                RoleScopes = [new UserRoleScope("Manager", Guid.NewGuid(), Guid.NewGuid(), null)]
            }
        });

        Assert.False(result.Succeeded);
        Assert.Equal(403, result.StatusCode);
        await store.DidNotReceive().ListAssignableAccountsAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
