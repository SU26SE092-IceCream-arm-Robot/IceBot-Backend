using Application.Identity.Workforce.Staff;
using Application.Shared.Wrappers;
using Domain.Identity.Entities;
using Application.Identity.Tokens.Claims;
using NSubstitute;

namespace IceBot.UnitTests.Identity;

public sealed class StaffWorkforceCommandTests
{
    [Fact]
    public async Task UpdateProfile_RejectsStaleWorkforceRevision()
    {
        var organizationId = Guid.NewGuid();
        var account = StaffAccount(organizationId, workforceRevision: 2);
        var accounts = Substitute.For<IStaffWorkforceStore>();
        ExecuteTransactionsInline(accounts);
        accounts.GetByIdAsync(account.Id, false, Arg.Any<CancellationToken>()).Returns(account);
        var handler = new UpdateStaffWorkforceCommandHandler(accounts);

        var result = await handler.HandleAsync(new UpdateStaffWorkforceCommand
        {
            OrganizationId = organizationId,
            AccountId = account.Id,
            ActorAccountId = Guid.NewGuid(),
            UserContext = Manager(organizationId),
            Request = new UpdateStaffWorkforceRequest { FullName = "Updated", ExpectedRevision = 1 }
        });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        await accounts.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProfile_RejectsMixedRoleTarget()
    {
        var organizationId = Guid.NewGuid();
        var account = StaffAccount(organizationId, workforceRevision: 1);
        account.AccountRoles.Add(new AccountRole
        {
            IsActive = true,
            OrganizationId = organizationId,
            Role = new Role { Code = "Manager" }
        });
        var accounts = Substitute.For<IStaffWorkforceStore>();
        ExecuteTransactionsInline(accounts);
        accounts.GetByIdAsync(account.Id, false, Arg.Any<CancellationToken>()).Returns(account);
        var handler = new UpdateStaffWorkforceCommandHandler(accounts);

        var result = await handler.HandleAsync(new UpdateStaffWorkforceCommand
        {
            OrganizationId = organizationId,
            AccountId = account.Id,
            ActorAccountId = Guid.NewGuid(),
            UserContext = Manager(organizationId),
            Request = new UpdateStaffWorkforceRequest { FullName = "Updated", ExpectedRevision = 1 }
        });

        Assert.False(result.Succeeded);
        Assert.Equal(403, result.StatusCode);
    }

    private static Account StaffAccount(Guid organizationId, long workforceRevision) => new()
    {
        Id = Guid.NewGuid(),
        UserName = "staff", Email = "staff@example.test", WorkforceRevision = workforceRevision,
        AccountRoles = [new AccountRole { IsActive = true, OrganizationId = organizationId, Role = new Role { Code = "Staff" } }]
    };

    private static CurrentUserContext Manager(Guid organizationId) => new()
    {
        AccountId = Guid.NewGuid(),
        RoleScopes = [new UserRoleScope("Manager", organizationId, null, null)]
    };

    private static void ExecuteTransactionsInline(IStaffWorkforceStore accounts)
    {
        accounts.ExecuteTransactionAsync(Arg.Any<Func<Task<ApiResult<StaffWorkforceResult>>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<Task<ApiResult<StaffWorkforceResult>>>>()());
        accounts.AcquireAccountLockAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }
}
