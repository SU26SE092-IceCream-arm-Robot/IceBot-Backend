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
    }
}
