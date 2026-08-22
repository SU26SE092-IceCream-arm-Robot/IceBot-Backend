using Domain.Common.Enums;
using Domain.Devices.ExecutionEndpoints;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Testcontainers.PostgreSql;

namespace IceBot.IntegrationTests.Devices;

public sealed class ExecutionEndpointReportedDevicesMigrationTests
{
    [IntegrationFact]
    public async Task Upgrade_discards_manual_targets_without_fabricating_reported_devices()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17")
            .WithDatabase("IceBotHardwareReportMigration")
            .WithUsername("postgres")
            .WithPassword("integration-password")
            .Build();
        await postgres.StartAsync();

        var options = new DbContextOptionsBuilder<IceBotDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;
        await using var db = new IceBotDbContext(options);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260813142051_AddStaffWorkforceManagement");

        var organization = new Organization
        {
            Code = $"REPORT-{Guid.NewGuid():N}",
            Name = "Hardware report migration organization",
            Status = EntityStatus.Active
        };
        var store = new Store
        {
            OrganizationId = organization.Id,
            Code = $"STORE-{Guid.NewGuid():N}",
            Name = "Hardware report migration store",
            Status = EntityStatus.Active
        };
        var kiosk = new Kiosk
        {
            OrganizationId = organization.Id,
            StoreId = store.Id,
            Code = $"KIOSK-{Guid.NewGuid():N}",
            Name = "Hardware report migration kiosk",
            Status = KioskStatus.Active
        };
        db.AddRange(organization, store, kiosk);
        await db.SaveChangesAsync();

        var endpointId = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "KioskExecutionEndpoints"
                ("Id", "KioskId", "EndpointCode", "ExecutionProfile", "AuthenticationMode", "Status", "CreatedAt")
            VALUES
                ({endpointId}, {kiosk.Id}, {"migration-edge"}, {(int)KioskExecutionProfile.FullEdge},
                 {(int)ExecutionEndpointAuthenticationMode.MutualTls}, {1}, {DateTimeOffset.UtcNow});
            """);

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "ExecutionEndpointSupportedRobotTargets"
                ("Id", "KioskExecutionEndpointId", "KioskId", "RuntimeTargetCode", "MachineModelCode", "CreatedAt")
            VALUES
                ({Guid.NewGuid()}, {endpointId}, {kiosk.Id}, {"FAIRINO_LUA_V1"}, {"FR5"}, {DateTimeOffset.UtcNow});
            """);

        await migrator.MigrateAsync();

        Assert.Empty(await db.ExecutionEndpointReportedDevices.AsNoTracking().ToListAsync());
        var oldTable = await db.Database
            .SqlQueryRaw<string>("SELECT to_regclass('\"ExecutionEndpointSupportedRobotTargets\"')::text AS \"Value\"")
            .SingleAsync();
        Assert.Null(oldTable);
    }
}
