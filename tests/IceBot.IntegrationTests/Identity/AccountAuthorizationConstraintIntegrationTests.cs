using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Tenants.Entities;
using IceBot.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IceBot.IntegrationTests.Identity;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class AccountAuthorizationConstraintIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task ActiveStoreGrant_RejectsDuplicateWhenKioskScopeIsNull()
    {
        await using var db = fixture.CreateDbContext();
        var graph = await SeedScopeAsync(db);
        db.AccountRoles.Add(CreateStoreGrant(graph));
        await db.SaveChangesAsync();

        db.AccountRoles.Add(CreateStoreGrant(graph));
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.Equal(PostgresErrorCodes.UniqueViolation, FindPostgresException(exception).SqlState);
    }

    [IntegrationFact]
    public async Task StoreGrant_RejectsMissingOrganizationScope()
    {
        await using var db = fixture.CreateDbContext();
        var graph = await SeedScopeAsync(db);
        var invalid = CreateStoreGrant(graph);
        invalid.OrganizationId = null;
        db.AccountRoles.Add(invalid);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.Equal(PostgresErrorCodes.CheckViolation, FindPostgresException(exception).SqlState);
    }

    [IntegrationFact]
    public async Task TechnicianGrantAudit_RejectsUnknownAccount()
    {
        await using var db = fixture.CreateDbContext();
        db.TechnicianSupportGrantHistories.Add(new TechnicianSupportGrantHistory
        {
            AccountId = Guid.NewGuid(),
            Action = "ScopeReplaced",
            Reason = "Foreign-key verification",
            AuthorizationVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, FindPostgresException(exception).SqlState);
    }

    [IntegrationFact]
    public async Task TechnicianGrantAudit_RejectsUnknownActorAccount()
    {
        await using var db = fixture.CreateDbContext();
        var graph = await SeedScopeAsync(db);
        db.TechnicianSupportGrantHistories.Add(new TechnicianSupportGrantHistory
        {
            AccountId = graph.AccountId,
            ActorAccountId = Guid.NewGuid(),
            Action = "ScopeReplaced",
            Reason = "Actor foreign-key verification",
            AuthorizationVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, FindPostgresException(exception).SqlState);
    }

    [IntegrationFact]
    public async Task TechnicianScopeReplay_RejectsUnknownAccount()
    {
        await using var db = fixture.CreateDbContext();
        db.TechnicianSupportScopeReplays.Add(new TechnicianSupportScopeReplay
        {
            AccountId = Guid.NewGuid(),
            IdempotencyKey = $"unknown-{Guid.NewGuid():N}",
            RequestFingerprint = new string('a', 64),
            AuthorizationVersion = 1,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, FindPostgresException(exception).SqlState);
    }

    [IntegrationFact]
    public async Task AccountRole_RejectsPlatformTechnicianRole()
    {
        await using var db = fixture.CreateDbContext();
        var graph = await SeedScopeAsync(db);
        var technicianRole = await db.Roles.SingleOrDefaultAsync(role => role.Code == "Technician");
        if (technicianRole is null)
        {
            technicianRole = new Role
            {
                Code = "Technician",
                Name = "Technician",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Roles.Add(technicianRole);
            await db.SaveChangesAsync();
        }
        db.AccountRoles.Add(new AccountRole
        {
            AccountId = graph.AccountId,
            RoleId = technicianRole.Id,
            OrganizationId = graph.OrganizationId,
            StoreId = graph.StoreId,
            IsActive = true,
            AssignedAt = DateTimeOffset.UtcNow
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.Equal(PostgresErrorCodes.CheckViolation, FindPostgresException(exception).SqlState);
    }

    private static AccountRole CreateStoreGrant(ScopeGraph graph) => new()
    {
        AccountId = graph.AccountId,
        RoleId = graph.RoleId,
        OrganizationId = graph.OrganizationId,
        StoreId = graph.StoreId,
        IsActive = true,
        AssignedAt = DateTimeOffset.UtcNow
    };

    private static async Task<ScopeGraph> SeedScopeAsync(global::Infrastructure.Data.IceBotDbContext db)
    {
        var organization = new Organization
        {
            Code = $"AUTH-{Guid.NewGuid():N}",
            Name = "Authorization constraint organization"
        };
        var store = new Store
        {
            OrganizationId = organization.Id,
            Code = $"STORE-{Guid.NewGuid():N}",
            Name = "Authorization constraint store"
        };
        var account = new Account
        {
            UserName = $"auth-{Guid.NewGuid():N}",
            Email = $"auth-{Guid.NewGuid():N}@example.test",
            Status = AccountStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var role = new Role
        {
            Code = $"ConstraintRole-{Guid.NewGuid():N}",
            Name = "Constraint role",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.AddRange(organization, store, account, role);
        await db.SaveChangesAsync();
        return new ScopeGraph(account.Id, role.Id, organization.Id, store.Id);
    }

    private static PostgresException FindPostgresException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is PostgresException postgres) return postgres;
        }

        throw new Xunit.Sdk.XunitException("Expected a PostgreSQL constraint exception.");
    }

    private sealed record ScopeGraph(Guid AccountId, long RoleId, Guid OrganizationId, Guid StoreId);
}
