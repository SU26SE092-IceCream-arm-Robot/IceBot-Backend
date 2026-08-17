using Application.Identity.Abstractions;
using Application.Identity.InternalAccounts.Commands;
using Application.Identity.InternalAccounts.Requests;
using Application.Identity.Provisioning;
using Application.Email;
using Application.Identity.Tokens.Claims;
using Domain.Identity.Entities;
using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;

namespace IceBot.UnitTests.Identity;

public sealed class OrgAdminAccountManagementTests
{
    [Fact]
    public async Task CreateAccount_AllowsOrgAdminToCreatePeerOrgAdminInOwnOrganization()
    {
        var organizationId = Guid.NewGuid();
        var accounts = CreateStore();
        accounts.GetRoleByCodeAsync("OrgAdmin", Arg.Any<CancellationToken>())
            .Returns(new Role { Code = "OrgAdmin" });
        var handler = CreateHandler(accounts);

        var result = await handler.HandleAsync(CreateCommand(organizationId, organizationId, "OrgAdmin"));

        Assert.True(result.Succeeded, result.Message);
        await accounts.Received(1).AddAsync(
            Arg.Is<Account>(account => account.AccountRoles.Single().OrganizationId == organizationId &&
                account.AccountRoles.Single().Role.Code == "OrgAdmin" &&
                account.Status == Domain.Identity.Enums.AccountStatus.Active &&
                account.LocalLoginEnabled &&
                account.Password != null),
            Arg.Any<CancellationToken>());
        await accounts.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAccount_RejectsOrgAdminRoleOutsideActorOrganization()
    {
        var organizationId = Guid.NewGuid();
        var accounts = CreateStore();
        accounts.GetRoleByCodeAsync("Manager", Arg.Any<CancellationToken>())
            .Returns(new Role { Code = "Manager" });
        var handler = CreateHandler(accounts);

        var result = await handler.HandleAsync(CreateCommand(organizationId, Guid.NewGuid(), "Manager"));

        Assert.False(result.Succeeded);
        Assert.Equal(403, result.StatusCode);
        await accounts.DidNotReceive().AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
        await accounts.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAccount_RejectsOrgAdminAttemptToCreateSystemAdmin()
    {
        var organizationId = Guid.NewGuid();
        var accounts = CreateStore();
        accounts.GetRoleByCodeAsync("SystemAdmin", Arg.Any<CancellationToken>())
            .Returns(new Role { Code = "SystemAdmin" });
        var handler = CreateHandler(accounts);

        var result = await handler.HandleAsync(CreateCommand(organizationId, null, "SystemAdmin"));

        Assert.False(result.Succeeded);
        Assert.Equal(403, result.StatusCode);
        await accounts.DidNotReceive().AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisableAccount_RejectsOrgAdminWhenTargetAlsoBelongsToAnotherOrganization()
    {
        var actorOrganizationId = Guid.NewGuid();
        var target = new Account();
        target.AccountRoles.Add(new AccountRole
        {
            IsActive = true,
            OrganizationId = actorOrganizationId
        });
        target.AccountRoles.Add(new AccountRole
        {
            IsActive = true,
            OrganizationId = Guid.NewGuid()
        });
        var accounts = CreateStore();
        accounts.GetByIdAsync(target.Id, false, Arg.Any<CancellationToken>()).Returns(target);
        var handler = new DisableInternalAccountCommandHandler(accounts, null!);

        var result = await handler.HandleAsync(new DisableInternalAccountCommand
        {
            AccountId = target.Id,
            OrganizationId = actorOrganizationId,
            UserContext = new CurrentUserContext
            {
                AccountId = Guid.NewGuid(),
                RoleScopes = [new UserRoleScope("OrgAdmin", actorOrganizationId, null, null)]
            }
        });

        Assert.False(result.Succeeded);
        Assert.Equal(403, result.StatusCode);
        await accounts.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static IIdentityAccountStore CreateStore()
    {
        var accounts = Substitute.For<IIdentityAccountStore>();
        accounts.ExistsByEmailOrUserNameAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        accounts.GoogleEmailExistsAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        return accounts;
    }

    private static CreateInternalAccountCommandHandler CreateHandler(IIdentityAccountStore accounts)
    {
        var hasher = Substitute.For<IPasswordHasher>();
        hasher.HashPassword(Arg.Any<string>()).Returns("hashed-password");
        var credentials = new TenantAccountCredentialService(
            hasher,
            Substitute.For<IEmailSender>(),
            NullLogger<TenantAccountCredentialService>.Instance);
        return new CreateInternalAccountCommandHandler(accounts, credentials);
    }

    private static CreateInternalAccountCommand CreateCommand(
        Guid actorOrganizationId,
        Guid? targetOrganizationId,
        string roleCode) =>
        new()
        {
            CreatedByAccountId = Guid.NewGuid(),
            OrganizationId = actorOrganizationId,
            UserContext = new CurrentUserContext
            {
                AccountId = Guid.NewGuid(),
                RoleScopes = [new UserRoleScope("OrgAdmin", actorOrganizationId, null, null)]
            },
            UserRoles = ["OrgAdmin"],
            Request = new CreateInternalAccountRequest
            {
                UserName = "new-user",
                Email = "new-user@example.com",
                GoogleLoginEnabled = true,
                GoogleEmail = "new-user@example.com",
                CreateInvitation = false,
                Roles = [new AccountRoleScopeRequest { RoleCode = roleCode, OrganizationId = targetOrganizationId }]
            }
        };
}
