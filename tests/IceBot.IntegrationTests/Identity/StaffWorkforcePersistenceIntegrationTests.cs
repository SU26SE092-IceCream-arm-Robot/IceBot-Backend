using Application.Email;
using Application.Identity.Invitations.Services;
using Application.Identity.Tokens.Claims;
using Application.Identity.Tokens.Services;
using Application.Identity.Workforce.Staff;
using Application.Tenants.Abstractions;
using Domain.Common.Enums;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Identity.Jobs;
using Infrastructure.Identity.Persistence;
using Infrastructure.Tenants.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IceBot.IntegrationTests.Identity;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class StaffWorkforcePersistenceIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task ConcurrentDuplicateCreate_ReplaysOnePersistedStaffAccount()
    {
        var graph = await SeedGraphAsync();
        var key = $"staff-create-{Guid.NewGuid():N}";
        var request = new CreateStaffWorkforceRequest
        {
            UserName = $"staff-{Guid.NewGuid():N}",
            Email = $"staff-{Guid.NewGuid():N}@example.test",
            FullName = "Concurrent staff",
            SendInvitationEmail = false,
            StaffScopes = [new StaffWorkforceScopeRequest { StoreId = graph.StoreAId }]
        };

        var results = await Task.WhenAll(CreateAsync(graph, request, key), CreateAsync(graph, request, key));

        Assert.All(results, result => Assert.True(result.Succeeded, result.Message));
        Assert.Single(results.Select(result => result.Data!.AccountId).Distinct());
        Assert.Contains(results, result => result.StatusCode == 201);
        Assert.Contains(results, result => result.StatusCode == 200);
        var sequentialReplay = await CreateAsync(graph, request, key);
        Assert.True(sequentialReplay.Succeeded, sequentialReplay.Message);
        Assert.Equal(200, sequentialReplay.StatusCode);
        Assert.Equal(results[0].Data!.AccountId, sequentialReplay.Data!.AccountId);

        await using var assertion = fixture.CreateDbContext();
        var accountId = results[0].Data!.AccountId;
        Assert.Equal(1, await assertion.Accounts.CountAsync(account => account.Id == accountId));
        Assert.Equal(1, await assertion.StaffWorkforceCreateReplays.CountAsync(replay => replay.OrganizationId == graph.OrganizationId && replay.IdempotencyKey == key));
        Assert.Equal(1, await assertion.AccountInvitations.CountAsync(invitation => invitation.AccountId == accountId));
    }

    [IntegrationFact]
    public async Task ConcurrentCreate_WithDifferentKeysAndSameEmail_ReturnsConflictWithoutDuplicateAccount()
    {
        var graph = await SeedGraphAsync();
        var email = $"staff-{Guid.NewGuid():N}@example.test";
        var firstRequest = new CreateStaffWorkforceRequest
        {
            UserName = $"staff-a-{Guid.NewGuid():N}",
            Email = email,
            SendInvitationEmail = false,
            StaffScopes = [new StaffWorkforceScopeRequest { StoreId = graph.StoreAId }]
        };
        var secondRequest = new CreateStaffWorkforceRequest
        {
            UserName = $"staff-b-{Guid.NewGuid():N}",
            Email = email,
            SendInvitationEmail = false,
            StaffScopes = [new StaffWorkforceScopeRequest { StoreId = graph.StoreAId }]
        };

        var results = await Task.WhenAll(
            CreateAsync(graph, firstRequest, $"staff-create-{Guid.NewGuid():N}"),
            CreateAsync(graph, secondRequest, $"staff-create-{Guid.NewGuid():N}"));

        Assert.Single(results, result => result.Succeeded && result.StatusCode == 201);
        Assert.Single(results, result => !result.Succeeded && result.StatusCode == 409);

        await using var assertion = fixture.CreateDbContext();
        Assert.Equal(1, await assertion.Accounts.CountAsync(account => account.Email == email));
    }

    [IntegrationFact]
    public async Task CreateReplay_ReturnsPersistedResult_WhenOriginalScopeIsNoLongerActive()
    {
        var graph = await SeedGraphAsync();
        var key = $"staff-replay-{Guid.NewGuid():N}";
        var request = new CreateStaffWorkforceRequest
        {
            UserName = $"staff-{Guid.NewGuid():N}",
            Email = $"staff-{Guid.NewGuid():N}@example.test",
            SendInvitationEmail = false,
            StaffScopes = [new StaffWorkforceScopeRequest { StoreId = graph.StoreAId }]
        };

        var created = await CreateAsync(graph, request, key);
        Assert.Equal(201, created.StatusCode);
        var accountId = created.Data!.AccountId;

        await using (var mutation = fixture.CreateDbContext())
        {
            var store = await mutation.Stores.SingleAsync(candidate => candidate.Id == graph.StoreAId);
            store.Status = EntityStatus.Inactive;
            await mutation.SaveChangesAsync();
        }

        var replay = await CreateAsync(graph, request, key);
        Assert.True(replay.Succeeded, replay.Message);
        Assert.Equal(200, replay.StatusCode);
        Assert.Equal(accountId, replay.Data!.AccountId);
    }

    [IntegrationFact]
    public async Task CreateReplay_RecreatesMissingInvitationWithoutDuplicatingStaffAccount()
    {
        var graph = await SeedGraphAsync();
        var key = $"staff-invitation-replay-{Guid.NewGuid():N}";
        var request = new CreateStaffWorkforceRequest
        {
            UserName = $"staff-{Guid.NewGuid():N}",
            Email = $"staff-{Guid.NewGuid():N}@example.test",
            SendInvitationEmail = false,
            StaffScopes = [new StaffWorkforceScopeRequest { StoreId = graph.StoreAId }]
        };

        var created = await CreateAsync(graph, request, key);
        Assert.Equal(201, created.StatusCode);
        var accountId = created.Data!.AccountId;

        await using (var mutation = fixture.CreateDbContext())
        {
            var invitation = await mutation.AccountInvitations.SingleAsync(candidate => candidate.AccountId == accountId);
            mutation.AccountInvitations.Remove(invitation);
            await mutation.SaveChangesAsync();
        }

        var replays = await Task.WhenAll(CreateAsync(graph, request, key), CreateAsync(graph, request, key));
        Assert.All(replays, replay => Assert.True(replay.Succeeded, replay.Message));
        Assert.All(replays, replay => Assert.Equal(200, replay.StatusCode));
        Assert.All(replays, replay => Assert.Equal(accountId, replay.Data!.AccountId));
        await using var assertion = fixture.CreateDbContext();
        Assert.Equal(1, await assertion.Accounts.CountAsync(candidate => candidate.Id == accountId));
        Assert.Equal(1, await assertion.AccountInvitations.CountAsync(candidate => candidate.AccountId == accountId));
    }

    [IntegrationFact]
    public async Task ManagerCannotAssignKioskFromAnotherStore_AndCanAssignKioskInOwnStore()
    {
        var graph = await SeedGraphAsync();
        var context = ManagerContext(graph);
        await using (var deniedDb = fixture.CreateDbContext())
        {
            var denied = await new UpdateStaffWorkforceScopesCommandHandler(
                new IdentityAccountStore(deniedDb), new TenantTreeStore(deniedDb))
                .HandleAsync(new UpdateStaffWorkforceScopesCommand
                {
                    OrganizationId = graph.OrganizationId,
                    AccountId = graph.StaffId,
                    ActorAccountId = graph.ManagerId,
                    UserContext = context,
                    Request = new UpdateStaffWorkforceScopesRequest
                    {
                        ExpectedRevision = 0,
                        StaffScopes = [new StaffWorkforceScopeRequest { StoreId = graph.StoreAId, KioskId = graph.KioskBId }]
                    }
                });
            Assert.False(denied.Succeeded);
            Assert.Equal(403, denied.StatusCode);
        }

        await using (var allowedDb = fixture.CreateDbContext())
        {
            var allowed = await new UpdateStaffWorkforceScopesCommandHandler(
                new IdentityAccountStore(allowedDb), new TenantTreeStore(allowedDb))
                .HandleAsync(new UpdateStaffWorkforceScopesCommand
                {
                    OrganizationId = graph.OrganizationId,
                    AccountId = graph.StaffId,
                    ActorAccountId = graph.ManagerId,
                    UserContext = context,
                    Request = new UpdateStaffWorkforceScopesRequest
                    {
                        ExpectedRevision = 0,
                        StaffScopes = [new StaffWorkforceScopeRequest { StoreId = graph.StoreAId, KioskId = graph.KioskAId }]
                    }
                });
            Assert.True(allowed.Succeeded, allowed.Message);
        }

        await using (var replayDb = fixture.CreateDbContext())
        {
            var replay = await new UpdateStaffWorkforceScopesCommandHandler(
                new IdentityAccountStore(replayDb), new TenantTreeStore(replayDb))
                .HandleAsync(new UpdateStaffWorkforceScopesCommand
                {
                    OrganizationId = graph.OrganizationId,
                    AccountId = graph.StaffId,
                    ActorAccountId = graph.ManagerId,
                    UserContext = context,
                    Request = new UpdateStaffWorkforceScopesRequest
                    {
                        ExpectedRevision = 1,
                        StaffScopes = [new StaffWorkforceScopeRequest { StoreId = graph.StoreAId, KioskId = graph.KioskAId }]
                    }
                });
            Assert.True(replay.Succeeded, replay.Message);
        }

        await using var assertion = fixture.CreateDbContext();
        var activeRoles = await assertion.AccountRoles.Where(role => role.AccountId == graph.StaffId && role.IsActive)
            .ToListAsync();
        Assert.Single(activeRoles);
        Assert.Equal(graph.KioskAId, activeRoles[0].KioskId);
        Assert.Equal(2, await assertion.AccountRoles.CountAsync(role => role.AccountId == graph.StaffId));
    }

    [IntegrationFact]
    public async Task ConcurrentProfileUpdate_WithSameRevision_AllowsOneAndRejectsOneAsStale()
    {
        var graph = await SeedGraphAsync();
        var command = new UpdateStaffWorkforceCommand
        {
            OrganizationId = graph.OrganizationId,
            AccountId = graph.StaffId,
            ActorAccountId = graph.ManagerId,
            UserContext = ManagerContext(graph),
            Request = new UpdateStaffWorkforceRequest { FullName = "Concurrent update", ExpectedRevision = 0 }
        };

        var results = await Task.WhenAll(UpdateAsync(command), UpdateAsync(command));

        Assert.Single(results, result => result.Succeeded);
        Assert.Single(results, result => !result.Succeeded && result.StatusCode == 409);
        await using var assertion = fixture.CreateDbContext();
        var account = await assertion.Accounts.SingleAsync(candidate => candidate.Id == graph.StaffId);
        Assert.Equal(1, account.WorkforceRevision);
    }

    [IntegrationFact]
    public async Task SessionRevocationFailure_LeavesDurableDisabledStateForReconciliation()
    {
        var graph = await SeedGraphAsync();
        await using (var mutation = fixture.CreateDbContext())
        {
            mutation.RefreshTokens.Add(new RefreshToken
            {
                AccountId = graph.StaffId,
                TokenHash = $"token-{Guid.NewGuid():N}",
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
            });
            await mutation.SaveChangesAsync();
        }

        await using (var lifecycleDb = fixture.CreateDbContext())
        {
            var handler = new ChangeStaffWorkforceLifecycleCommandHandler(
                new IdentityAccountStore(lifecycleDb),
                new ThrowingSessionRevoker(),
                NullLogger<ChangeStaffWorkforceLifecycleCommandHandler>.Instance);
            var disabled = await handler.HandleAsync(new ChangeStaffWorkforceLifecycleCommand
            {
                OrganizationId = graph.OrganizationId,
                AccountId = graph.StaffId,
                ActorAccountId = graph.ManagerId,
                UserContext = ManagerContext(graph),
                Request = new StaffLifecycleRequest
                {
                    Reason = "Integration session failure",
                    ExpectedRevision = 0,
                    IdempotencyKey = $"disable-{Guid.NewGuid():N}"
                }
            });
            Assert.True(disabled.Succeeded, disabled.Message);
            Assert.Equal(202, disabled.StatusCode);
            Assert.Equal("pending", disabled.Details!["sessionRevocation"]);
        }

        await using (var pendingAssertion = fixture.CreateDbContext())
        {
            Assert.Equal(AccountStatus.Disabled, await pendingAssertion.Accounts
                .Where(account => account.Id == graph.StaffId)
                .Select(account => account.Status)
                .SingleAsync());
            Assert.Equal(1, await pendingAssertion.StaffWorkforceLifecycleTransitions
                .CountAsync(transition => transition.AccountId == graph.StaffId && transition.ToStatus == AccountStatus.Disabled));
            Assert.Equal(1, await pendingAssertion.RefreshTokens.CountAsync(token => token.AccountId == graph.StaffId && token.RevokedAt == null));
        }

        await using (var reconciliationDb = fixture.CreateDbContext())
        {
            var reconciler = new StaffSessionRevocationReconciler(
                new IdentityAccountStore(reconciliationDb),
                new RefreshTokenStaffSessionRevoker(new RefreshTokenService(new RefreshTokenStore(reconciliationDb))),
                NullLogger<StaffSessionRevocationReconciler>.Instance);
            Assert.Equal(1, await reconciler.ReconcileAsync(10));
        }

        await using var assertion = fixture.CreateDbContext();
        Assert.Equal(0, await assertion.RefreshTokens.CountAsync(token => token.AccountId == graph.StaffId && token.RevokedAt == null));
    }

    private async Task<Application.Shared.Wrappers.ApiResult<StaffWorkforceResult>> CreateAsync(
        Graph graph, CreateStaffWorkforceRequest request, string key)
    {
        await using var db = fixture.CreateDbContext();
        var invitationService = new AccountInvitationService(
            new AccountInvitationStore(db),
            new NoopEmailSender(),
            Options.Create(new EmailOptions { InvitationBaseUrl = "https://example.test/invitations" }),
            NullLogger<AccountInvitationService>.Instance);
        return await new CreateStaffWorkforceCommandHandler(
            new IdentityAccountStore(db), new TenantTreeStore(db), invitationService)
            .HandleAsync(new CreateStaffWorkforceCommand
            {
                OrganizationId = graph.OrganizationId,
                ActorAccountId = graph.ManagerId,
                IdempotencyKey = key,
                UserContext = ManagerContext(graph),
                Request = request
            });
    }

    private async Task<Application.Shared.Wrappers.ApiResult<StaffWorkforceResult>> UpdateAsync(UpdateStaffWorkforceCommand command)
    {
        await using var db = fixture.CreateDbContext();
        return await new UpdateStaffWorkforceCommandHandler(new IdentityAccountStore(db)).HandleAsync(command);
    }

    private async Task<Graph> SeedGraphAsync()
    {
        await using var db = fixture.CreateDbContext();
        var organization = new Organization { Code = $"STAFF-{Guid.NewGuid():N}", Name = "Staff workforce", Status = EntityStatus.Active };
        var storeA = new Store { OrganizationId = organization.Id, Code = $"STORE-A-{Guid.NewGuid():N}", Name = "Store A", Status = EntityStatus.Active };
        var storeB = new Store { OrganizationId = organization.Id, Code = $"STORE-B-{Guid.NewGuid():N}", Name = "Store B", Status = EntityStatus.Active };
        var kioskA = new Kiosk { OrganizationId = organization.Id, StoreId = storeA.Id, Code = $"KIOSK-A-{Guid.NewGuid():N}", Name = "Kiosk A", Status = KioskStatus.Active };
        var kioskB = new Kiosk { OrganizationId = organization.Id, StoreId = storeB.Id, Code = $"KIOSK-B-{Guid.NewGuid():N}", Name = "Kiosk B", Status = KioskStatus.Active };
        var managerRole = new Role { Code = $"Manager-{Guid.NewGuid():N}", Name = "Manager", IsActive = true };
        var staffRole = await db.Roles.SingleOrDefaultAsync(role => role.Code == "Staff")
            ?? new Role { Code = "Staff", Name = "Staff", IsActive = true };
        var manager = NewAccount("manager");
        var staff = NewAccount("staff");
        manager.AccountRoles.Add(new AccountRole { Role = managerRole, OrganizationId = organization.Id, StoreId = storeA.Id, AssignedAt = DateTimeOffset.UtcNow });
        staff.AccountRoles.Add(new AccountRole { Role = staffRole, OrganizationId = organization.Id, StoreId = storeA.Id, AssignedAt = DateTimeOffset.UtcNow });

        db.AddRange(organization, storeA, storeB, kioskA, kioskB, managerRole, manager, staff);
        if (staffRole.Id == 0) db.Roles.Add(staffRole);
        await db.SaveChangesAsync();
        return new Graph(organization.Id, storeA.Id, storeB.Id, kioskA.Id, kioskB.Id, manager.Id, staff.Id);
    }

    private static Account NewAccount(string prefix) => new()
    {
        UserName = $"{prefix}-{Guid.NewGuid():N}",
        Email = $"{prefix}-{Guid.NewGuid():N}@example.test",
        Status = AccountStatus.Active,
        LocalLoginEnabled = true
    };

    private static CurrentUserContext ManagerContext(Graph graph) => new()
    {
        AccountId = graph.ManagerId,
        RoleScopes = [new UserRoleScope("Manager", graph.OrganizationId, graph.StoreAId, null)]
    };

    private sealed record Graph(Guid OrganizationId, Guid StoreAId, Guid StoreBId, Guid KioskAId, Guid KioskBId, Guid ManagerId, Guid StaffId);

    private sealed class NoopEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ThrowingSessionRevoker : IStaffSessionRevoker
    {
        public Task<int> RevokeAllAsync(Guid accountId, string reason, CancellationToken cancellationToken = default) =>
            Task.FromException<int>(new InvalidOperationException("Synthetic revocation failure."));
    }
}
