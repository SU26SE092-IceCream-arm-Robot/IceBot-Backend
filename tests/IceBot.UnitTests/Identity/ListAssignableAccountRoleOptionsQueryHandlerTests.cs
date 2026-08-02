using Application.Identity.Abstractions;
using Application.Identity.Roles.Queries;
using Application.Identity.Tokens.Claims;
using Domain.Identity.Entities;
using NSubstitute;

namespace IceBot.UnitTests.Identity;

public sealed class ListAssignableAccountRoleOptionsQueryHandlerTests
{
    [Fact]
    public async Task OrgAdmin_GetsOnlyRolesItCanAssign()
    {
        var store = Substitute.For<IIdentityAccountStore>();
        store.ListActiveRolesAsync(Arg.Any<CancellationToken>()).Returns(
        [
            Role("SystemAdmin"),
            Role("OrgAdmin"),
            Role("Manager"),
            Role("Staff"),
            Role("Technician")
        ]);
        var handler = new ListManagementRolesQueryHandler(store);

        var result = await handler.HandleAsync(new ListManagementRolesQuery
        {
            UserContext = new CurrentUserContext
            {
                AccountId = Guid.NewGuid(),
                RoleScopes = [new UserRoleScope("OrgAdmin", Guid.NewGuid(), null, null)]
            },
            UserRoles = ["OrgAdmin"]
        });

        Assert.True(result.Succeeded);
        var roles = result.Data!.ToArray();
        Assert.Equal(
            ["Manager", "OrgAdmin", "Staff", "Technician"],
            roles.Select(role => role.Code).OrderBy(code => code));
        Assert.All(roles, role => Assert.NotEmpty(role.AllowedScopeTypes));
    }

    private static Role Role(string code) => new()
    {
        Code = code,
        Name = code,
        IsActive = true
    };
}
