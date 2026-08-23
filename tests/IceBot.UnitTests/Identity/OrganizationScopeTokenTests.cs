using Application.Identity.Abstractions;
using Application.Identity;
using Application.Identity.Tokens.Claims;
using Application.Identity.Tokens.Services;
using Application.Shared.Wrappers;
using Domain.Common.Enums;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Tenants.Entities;
using NSubstitute;

namespace IceBot.UnitTests.Identity;

public sealed class OrganizationScopeTokenTests
{
    [Fact]
    public async Task IssueAsync_ExcludesSuspendedOrganizationScopeWhenAccountHasAnotherActiveScope()
    {
        var activeOrganizationId = Guid.NewGuid();
        var account = CreateAccount(
            CreateRole("OrgAdmin", new Organization { Id = Guid.NewGuid(), Status = EntityStatus.Suspended }),
            CreateRole("Manager", new Organization { Id = activeOrganizationId, Status = EntityStatus.Active }));
        var generator = Substitute.For<IAccessTokenGenerator>();
        generator.GenerateAccessToken(
                account.Id,
                Arg.Any<Guid>(),
                account.UserName,
                Arg.Any<IReadOnlyCollection<AccountRoleClaim>>(),
                AccountStatus.Active,
                account.AuthorizationVersion)
            .Returns(ApiResult<string>.Success("access-token"));
        var refreshStore = CreateRefreshStore();
        var service = new AccountTokenService(generator, new RefreshTokenService(refreshStore), Substitute.For<IIdentityAccountStore>());

        var result = await service.IssueAsync(account);

        Assert.True(result.Succeeded, result.Message);
        var role = Assert.Single(result.Data!.Roles);
        Assert.Equal("Manager", role.RoleCode);
        Assert.Equal(activeOrganizationId, role.OrganizationId);
    }

    [Fact]
    public async Task IssueAsync_RejectsAccountWithOnlySuspendedOrganizationScopeBeforeCreatingSession()
    {
        var account = CreateAccount(CreateRole(
            "OrgAdmin",
            new Organization { Id = Guid.NewGuid(), Status = EntityStatus.Suspended }));
        var refreshStore = CreateRefreshStore();
        var service = new AccountTokenService(
            Substitute.For<IAccessTokenGenerator>(),
            new RefreshTokenService(refreshStore),
            Substitute.For<IIdentityAccountStore>());

        var result = await service.IssueAsync(account);

        Assert.False(result.Succeeded);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal(IdentityErrors.OrganizationSuspended.Code, result.BusinessError);
        await refreshStore.DidNotReceive().AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IssueAsync_AllowsGlobalSystemAdminWithoutOrganizationScope()
    {
        var account = CreateAccount(CreateRole("SystemAdmin", organization: null));
        var generator = Substitute.For<IAccessTokenGenerator>();
        generator.GenerateAccessToken(
                account.Id,
                Arg.Any<Guid>(),
                account.UserName,
                Arg.Any<IReadOnlyCollection<AccountRoleClaim>>(),
                AccountStatus.Active,
                account.AuthorizationVersion)
            .Returns(ApiResult<string>.Success("access-token"));
        var service = new AccountTokenService(
            generator,
            new RefreshTokenService(CreateRefreshStore()),
            Substitute.For<IIdentityAccountStore>());

        var result = await service.IssueAsync(account);

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal("SystemAdmin", Assert.Single(result.Data!.Roles).RoleCode);
    }

    private static Account CreateAccount(params AccountRole[] roles) => new()
    {
        Id = Guid.NewGuid(),
        UserName = "operator",
        Email = "operator@example.com",
        Status = AccountStatus.Active,
        AccountRoles = roles
    };

    private static AccountRole CreateRole(string roleCode, Organization? organization) => new()
    {
        Id = Guid.NewGuid(),
        IsActive = true,
        OrganizationId = organization?.Id,
        Organization = organization,
        Role = new Role { Code = roleCode, Priority = 1, IsActive = true }
    };

    private static IRefreshTokenStore CreateRefreshStore()
    {
        var store = Substitute.For<IRefreshTokenStore>();
        store.ExecuteInTransactionAsync(
                Arg.Any<Func<Task<RefreshTokenIssue>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.ArgAt<Func<Task<RefreshTokenIssue>>>(0)());
        return store;
    }
}
