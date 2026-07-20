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

namespace IceBot.IntegrationTests.Operations;

public sealed class NotificationDeliveryMigrationUpgradeTests
{
    [IntegrationFact]
    public async Task ExistingOutboxRow_BackfillsTenantScopeFromKioskPayload()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17")
            .WithDatabase("IceBotMigrationUpgrade")
            .WithUsername("postgres")
            .WithPassword("integration-password")
            .Build();
        await postgres.StartAsync();
        var options = new DbContextOptionsBuilder<IceBotDbContext>()
            .UseNpgsql(postgres.GetConnectionString()).Options;
        await using var db = new IceBotDbContext(options);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260719214928_AddOperationalAutomationAndFranchiseOnboarding");

        var organization = new Organization
        {
            Code = $"MIGRATION-{Guid.NewGuid():N}", Name = "Migration organization", Status = EntityStatus.Active
        };
        var store = new Store
        {
            OrganizationId = organization.Id, Code = $"STORE-{Guid.NewGuid():N}",
            Name = "Migration store", Status = EntityStatus.Active
        };
        var kiosk = new Kiosk
        {
            OrganizationId = organization.Id, StoreId = store.Id,
            Code = $"KIOSK-{Guid.NewGuid():N}", Name = "Migration kiosk", Status = KioskStatus.Active
        };
        var account = new Account
        {
            UserName = $"migration-{Guid.NewGuid():N}", Email = $"migration-{Guid.NewGuid():N}@example.test",
            Status = AccountStatus.Active
        };
        db.AddRange(organization, store, kiosk, account);
        await db.SaveChangesAsync();

        var deliveryId = Guid.NewGuid();
        var alertId = Guid.NewGuid();
        var payload = $$"""{"kioskId":"{{kiosk.Id:D}}","alertId":"{{alertId:D}}"}""";
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "NotificationDeliveries"
                ("Id", "DeliveryKey", "NotificationType", "RecipientAccountId", "Title", "Body",
                 "DataJson", "Status", "AttemptCount", "MaxAttempts", "NextAttemptAt", "CreatedAt")
            VALUES
                ({deliveryId}, {$"legacy:{deliveryId:N}"}, {"critical_operational_alert"}, {account.Id},
                 {"Legacy alert"}, {"Legacy body"}, {payload}::jsonb, {1}, {0}, {5},
                 {DateTimeOffset.UtcNow}, {DateTimeOffset.UtcNow});
            """);

        await migrator.MigrateAsync();
        var delivery = await db.NotificationDeliveries.AsNoTracking().SingleAsync(x => x.Id == deliveryId);
        Assert.Equal(organization.Id, delivery.OrganizationId);
        Assert.Equal(store.Id, delivery.StoreId);
        Assert.Equal(kiosk.Id, delivery.KioskId);
        Assert.Equal(alertId, delivery.SubjectId);
    }
}
