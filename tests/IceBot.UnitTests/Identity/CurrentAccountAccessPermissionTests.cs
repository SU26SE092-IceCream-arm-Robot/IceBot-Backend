using Application.Identity.CurrentAccount.Queries;
using Application.Identity.Tokens.Claims;

namespace IceBot.UnitTests.Identity;

public sealed class CurrentAccountAccessPermissionTests
{
    [Fact]
    public async Task ManagerAccess_UsesPermissionCodesInsteadOfRoleNameInference()
    {
        var handler = new GetCurrentAccountAccessQueryHandler();
        var organizationId = Guid.NewGuid();

        var result = await handler.HandleAsync(new GetCurrentAccountAccessQuery
        {
            UserContext = new CurrentUserContext
            {
                AccountId = Guid.NewGuid(),
                RoleScopes = [new UserRoleScope("Manager", organizationId, null, null)]
            },
            RoleCodes = ["Manager"],
            RoleScopeClaims = [$"Manager|{organizationId}|*|*"]
        });

        Assert.True(result.Succeeded);
        Assert.Contains("maintenance.manage", result.Data!.PermissionCodes);
        Assert.Contains("menus.manage", result.Data.PermissionCodes);
        Assert.DoesNotContain("accounts.read", result.Data.PermissionCodes);
        Assert.DoesNotContain("permission-matrix.view", result.Data.PermissionCodes);

        var menuScope = Assert.Single(result.Data.PermissionScopes,
            permission => permission.PermissionCode == "menus.manage");
        Assert.True(menuScope.ScopeRequired);
        Assert.False(menuScope.IsGlobal);
        var scope = Assert.Single(menuScope.Scopes);
        Assert.Equal(organizationId, scope.OrganizationId);
        Assert.Null(scope.StoreId);
        Assert.Null(scope.KioskId);
    }

    [Fact]
    public async Task PermissionScopes_DoNotBorrowScopeFromAnotherRole()
    {
        var handler = new GetCurrentAccountAccessQueryHandler();
        var publishingOrganizationId = Guid.NewGuid();
        var managementOrganizationId = Guid.NewGuid();

        var result = await handler.HandleAsync(new GetCurrentAccountAccessQuery
        {
            UserContext = new CurrentUserContext
            {
                AccountId = Guid.NewGuid(),
                RoleScopes =
                [
                    new UserRoleScope("OrgAdmin", publishingOrganizationId, null, null),
                    new UserRoleScope("Manager", managementOrganizationId, null, null)
                ]
            },
            RoleCodes = ["OrgAdmin", "Manager"],
            RoleScopeClaims =
            [
                $"OrgAdmin|{publishingOrganizationId}|*|*",
                $"Manager|{managementOrganizationId}|*|*"
            ]
        });

        Assert.True(result.Succeeded);
        var publishScope = Assert.Single(result.Data!.PermissionScopes,
            permission => permission.PermissionCode == "release.publish");
        Assert.False(publishScope.IsGlobal);
        var scope = Assert.Single(publishScope.Scopes);
        Assert.Equal(publishingOrganizationId, scope.OrganizationId);
        Assert.DoesNotContain(publishScope.Scopes,
            item => item.OrganizationId == managementOrganizationId);

        var deployScope = Assert.Single(result.Data.PermissionScopes,
            permission => permission.PermissionCode == "release.deploy");
        Assert.Contains(deployScope.Scopes,
            item => item.OrganizationId == publishingOrganizationId);
        Assert.Contains(deployScope.Scopes,
            item => item.OrganizationId == managementOrganizationId);
    }

    [Fact]
    public async Task UnscopedPermission_IsReportedAsGlobalForScopedRole()
    {
        var handler = new GetCurrentAccountAccessQueryHandler();
        var organizationId = Guid.NewGuid();

        var result = await handler.HandleAsync(new GetCurrentAccountAccessQuery
        {
            UserContext = new CurrentUserContext
            {
                AccountId = Guid.NewGuid(),
                RoleScopes = [new UserRoleScope("Manager", organizationId, null, null)]
            },
            RoleCodes = ["Manager"],
            RoleScopeClaims = [$"Manager|{organizationId}|*|*"]
        });

        var catalogScope = Assert.Single(result.Data!.PermissionScopes,
            permission => permission.PermissionCode == "device-catalog.read");
        Assert.False(catalogScope.ScopeRequired);
        Assert.True(catalogScope.IsGlobal);
    }
}
