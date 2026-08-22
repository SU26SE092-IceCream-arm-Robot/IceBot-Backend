using Domain.Common.Enums;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Testcontainers.PostgreSql;

namespace IceBot.IntegrationTests.Identity;

public sealed class PlatformTechnicianProfileMigrationTests
{
    [IntegrationFact]
    public async Task Upgrade_NormalizesHistoricalKioskGrantAndBackfillsPlatformProfile()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17")
            .WithDatabase("IceBotTechnicianMigration")
            .WithUsername("postgres")
            .WithPassword("integration-password")
            .Build();
        await postgres.StartAsync();

        var options = new DbContextOptionsBuilder<IceBotDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;
        await using var db = new IceBotDbContext(options);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260822133741_AddTechnicianSupportGrantAudit");

        var organization = new Organization
        {
            Code = $"TECH-{Guid.NewGuid():N}",
            Name = "Technician migration organization",
            Status = EntityStatus.Active
        };
        var store = new Store
        {
            OrganizationId = organization.Id,
            Code = $"STORE-{Guid.NewGuid():N}",
            Name = "Technician migration store",
            Status = EntityStatus.Active
        };
        var kiosk = new Kiosk
        {
            OrganizationId = organization.Id,
            StoreId = store.Id,
            Code = $"KIOSK-{Guid.NewGuid():N}",
            Name = "Technician migration kiosk",
            Status = KioskStatus.Active
        };
        var role = new Role
        {
            Code = "Technician",
            Name = "Technician",
            IsSystemRole = true,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var account = new Account
        {
            UserName = $"migration-tech-{Guid.NewGuid():N}",
            Email = $"migration-tech-{Guid.NewGuid():N}@icebot.test",
            Status = AccountStatus.Active,
            LocalLoginEnabled = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var grant = new AccountRole
        {
            AccountId = account.Id,
            Role = role,
            OrganizationId = organization.Id,
            StoreId = store.Id,
            KioskId = kiosk.Id,
            IsActive = true,
            AssignedAt = DateTimeOffset.UtcNow
        };
        db.AddRange(organization, store, kiosk, role, account, grant);
        await db.SaveChangesAsync();

        await migrator.MigrateAsync();
        db.ChangeTracker.Clear();

        Assert.False(await db.AccountRoles.AnyAsync(candidate => candidate.AccountId == account.Id));
        var normalizedGrant = await db.TechnicianSupportGrants.SingleAsync(candidate => candidate.Id == grant.Id);
        Assert.Null(normalizedGrant.StoreId);
        Assert.Equal(kiosk.Id, normalizedGrant.KioskId);
        Assert.True(await db.PlatformTechnicianProfiles.AnyAsync(profile => profile.AccountId == account.Id));
    }
}
