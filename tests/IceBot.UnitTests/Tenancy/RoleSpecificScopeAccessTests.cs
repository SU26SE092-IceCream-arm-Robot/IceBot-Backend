using Application.Identity.Tokens.Claims;
using Application.Tenants;

namespace IceBot.UnitTests.Tenancy;

public sealed class RoleSpecificScopeAccessTests
{
    [Fact]
    public void StrongRoleCannotBorrowAnotherRolesKioskScope()
    {
        var managerOrganizationId = Guid.NewGuid();
        var staffOrganizationId = Guid.NewGuid();
        var staffStoreId = Guid.NewGuid();
        var staffKioskId = Guid.NewGuid();
        var context = new CurrentUserContext
        {
            RoleScopes =
            [
                new UserRoleScope("Manager", managerOrganizationId, null, null),
                new UserRoleScope("Staff", staffOrganizationId, staffStoreId, staffKioskId)
            ]
        };

        Assert.False(ScopeAccessRules.CanAccessScopedRow(
            ScopeRoleSets.DevicesManage,
            context,
            staffOrganizationId,
            staffStoreId,
            staffKioskId));
        Assert.True(ScopeAccessRules.CanAccessScopedRow(
            ScopeRoleSets.DevicesView,
            context,
            staffOrganizationId,
            staffStoreId,
            staffKioskId));
    }

    [Fact]
    public void EffectiveScopeContainsOnlyScopesFromRolesAllowedForOperation()
    {
        var managerOrganizationId = Guid.NewGuid();
        var technicianOrganizationId = Guid.NewGuid();
        var technicianStoreId = Guid.NewGuid();
        var technicianKioskId = Guid.NewGuid();
        var context = new CurrentUserContext
        {
            RoleScopes =
            [
                new UserRoleScope("Manager", managerOrganizationId, null, null),
                new UserRoleScope("Technician", technicianOrganizationId, technicianStoreId, technicianKioskId)
            ]
        };

        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.OrdersView, context);

        Assert.Contains(managerOrganizationId, scope.OrganizationIds);
        Assert.DoesNotContain(technicianKioskId, scope.KioskIds);
    }
}
