using Application.Identity.Tokens.Claims;
using Application.Operations.MaintenanceTickets.Rules;

namespace IceBot.UnitTests.Operations;

public sealed class MaintenanceTicketAccessRulesTests
{
    [Fact]
    public void ManagerWithoutTenantScope_CannotManageArbitraryTicket()
    {
        var user = new CurrentUserContext
        {
            RoleScopes =
            [
                new UserRoleScope("Manager", null, null, null)
            ]
        };

        Assert.False(MaintenanceTicketAccessRules.CanAssign(
            user, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void ManagerWithMatchingStoreScope_CanManageTicket()
    {
        var storeId = Guid.NewGuid();
        var user = new CurrentUserContext
        {
            RoleScopes =
            [
                new UserRoleScope("Manager", Guid.NewGuid(), storeId, null)
            ]
        };

        Assert.True(MaintenanceTicketAccessRules.CanAssign(
            user, Guid.NewGuid(), storeId, Guid.NewGuid()));
    }
}
