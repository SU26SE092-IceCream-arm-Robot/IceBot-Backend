using System.Security.Cryptography;
using Application.ClientDevices;
using Application.ClientDevices.Contracts;
using Application.ClientDevices.Security;
using Application.Identity.Tokens.Claims;
using Domain.Common.Enums;
using Domain.Devices.ClientDevices;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Orders.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Data;
using Infrastructure.Devices.ClientDevices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace IceBot.IntegrationTests.Devices;

public sealed class ClientDeviceMigrationTests
{
    [IntegrationFact]
    public async Task Upgrade_backfills_historical_tablet_orders_to_one_retired_client_device_per_kiosk()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17")
            .WithDatabase("IceBotClientDeviceMigration")
            .WithUsername("postgres")
            .WithPassword("integration-password")
            .Build();
        await postgres.StartAsync();

        var options = new DbContextOptionsBuilder<IceBotDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;
        await using var db = new IceBotDbContext(options);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260822155255_NormalizeOperationalOwnershipBoundaries");

        var graph = await SeedKioskAsync(db, "migration");
        var now = DateTimeOffset.UtcNow;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Orders" (
                "Id", "OrganizationId", "StoreId", "KioskId", "OrderNumber", "Channel", "Status", "PaymentStatus",
                "Currency", "SubtotalAmount", "DiscountAmount", "TaxAmount", "TotalAmount", "PaidAmount",
                "PlacedAt", "PaymentDeadlineAt", "CreatedAt")
            VALUES
                ({Guid.NewGuid()}, {graph.OrganizationId}, {graph.StoreId}, {graph.KioskId}, {"historical-tablet-one"},
                 {(int)OrderChannel.Tablet}, {(int)OrderStatus.PendingPayment}, {(int)PaymentStatus.Unpaid},
                 {"VND"}, {35000m}, {0m}, {0m}, {35000m}, {0m}, {now}, {now.AddMinutes(15)}, {now}),
                ({Guid.NewGuid()}, {graph.OrganizationId}, {graph.StoreId}, {graph.KioskId}, {"historical-tablet-two"},
                 {(int)OrderChannel.Tablet}, {(int)OrderStatus.PendingPayment}, {(int)PaymentStatus.Unpaid},
                 {"VND"}, {35000m}, {0m}, {0m}, {35000m}, {0m}, {now}, {now.AddMinutes(15)}, {now});
            """);

        await migrator.MigrateAsync();
        db.ChangeTracker.Clear();

        var historicalDevice = await db.ClientDevices.AsNoTracking()
            .SingleAsync(device => device.KioskId == graph.KioskId);
        Assert.Equal(ClientDeviceType.SelfOrderTablet, historicalDevice.Type);
        Assert.Equal(ClientDeviceStatus.Retired, historicalDevice.Status);
        Assert.Empty(await db.ClientDeviceCredentials.AsNoTracking()
            .Where(credential => credential.ClientDeviceId == historicalDevice.Id)
            .ToListAsync());

        var sourceIds = await db.Orders.AsNoTracking()
            .Where(order => order.KioskId == graph.KioskId && order.Channel == OrderChannel.Tablet)
            .Select(order => order.SourceClientDeviceId)
            .Distinct()
            .ToListAsync();
        Assert.Equal([historicalDevice.Id], sourceIds);
    }

    private static async Task<SeededKiosk> SeedKioskAsync(IceBotDbContext db, string prefix)
    {
        var organization = new Organization
        {
            Code = $"CLIENT-{prefix}-{Guid.NewGuid():N}",
            Name = "Client-device migration organization",
            Status = EntityStatus.Active
        };
        var store = new Store
        {
            OrganizationId = organization.Id,
            Code = $"STORE-{prefix}-{Guid.NewGuid():N}",
            Name = "Client-device migration store",
            Status = EntityStatus.Active
        };
        var kiosk = new Kiosk
        {
            OrganizationId = organization.Id,
            StoreId = store.Id,
            Code = $"KIOSK-{prefix}-{Guid.NewGuid():N}",
            Name = "Client-device migration kiosk",
            Status = KioskStatus.Active
        };
        db.AddRange(organization, store, kiosk);
        await db.SaveChangesAsync();
        return new SeededKiosk(organization.Id, store.Id, kiosk.Id);
    }

    private sealed record SeededKiosk(Guid OrganizationId, Guid StoreId, Guid KioskId);
}

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class ClientDeviceManagementPostgresIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task Concurrent_replace_with_the_same_key_replays_one_atomic_slot_replacement()
    {
        var graph = await SeedKioskAsync();
        var actor = new CurrentUserContext { AccountId = graph.ActorAccountId, IsSystemAdmin = true };
        var currentCredential = Credential();

        Guid currentDeviceId;
        int currentRevision;
        await using (var provisionDb = fixture.CreateDbContext())
        {
            var provision = await CreateService(provisionDb).ProvisionAsync(
                graph.KioskId,
                new ProvisionClientDeviceRequest(Guid.NewGuid(), currentCredential, "Current tablet", "1.0.0", "windows", "Initial setup"),
                "client-device-initial-provision",
                actor);
            Assert.True(provision.Succeeded, provision.Message);
            currentDeviceId = provision.Data!.Id;
            currentRevision = provision.Data.Revision;
        }

        var replacementInstallationId = Guid.NewGuid();
        var replacementCredential = Credential();
        var request = new ReplaceClientDeviceRequest(
            currentDeviceId,
            currentRevision,
            replacementInstallationId,
            replacementCredential,
            "Replacement tablet",
            "1.1.0",
            "windows",
            "Replace failed tablet");

        await using var firstDb = fixture.CreateDbContext();
        await using var secondDb = fixture.CreateDbContext();
        var first = CreateService(firstDb).ReplaceAsync(graph.KioskId, request, "client-device-replace-replay", actor);
        var second = CreateService(secondDb).ReplaceAsync(graph.KioskId, request, "client-device-replace-replay", actor);
        var results = await Task.WhenAll(first, second);

        Assert.All(results, result => Assert.True(result.Succeeded, result.Message));
        Assert.Equal(results[0].Data!.Id, results[1].Data!.Id);

        await using var assertionDb = fixture.CreateDbContext();
        var devices = await assertionDb.ClientDevices.AsNoTracking()
            .Where(device => device.KioskId == graph.KioskId)
            .OrderBy(device => device.CreatedAt)
            .ToListAsync();
        Assert.Equal(2, devices.Count);
        Assert.Equal(ClientDeviceStatus.Retired, devices.Single(device => device.Id == currentDeviceId).Status);
        var replacement = devices.Single(device => device.Id == results[0].Data!.Id);
        Assert.Equal(ClientDeviceStatus.Active, replacement.Status);
        Assert.Equal(replacementInstallationId, replacement.InstallationId);
        Assert.Single(await assertionDb.ClientDeviceOperationReplays.AsNoTracking()
            .Where(replay => replay.KioskId == graph.KioskId && replay.Operation == "Replace" &&
                             replay.IdempotencyKey == "client-device-replace-replay")
            .ToListAsync());

        var staleReplace = await CreateService(assertionDb).ReplaceAsync(
            graph.KioskId,
            request with { ReplacementInstallationId = Guid.NewGuid(), Credential = Credential() },
            "client-device-replace-stale-observation",
            actor);
        Assert.False(staleReplace.Succeeded);
        Assert.Equal(409, staleReplace.StatusCode);
        Assert.Equal(2, await assertionDb.ClientDevices.CountAsync(device => device.KioskId == graph.KioskId));
    }

    [IntegrationFact]
    public async Task Provision_replay_returns_the_original_device_and_rejects_a_changed_payload()
    {
        var graph = await SeedKioskAsync();
        var actor = new CurrentUserContext { AccountId = graph.ActorAccountId, IsSystemAdmin = true };
        var request = new ProvisionClientDeviceRequest(
            Guid.NewGuid(), Credential(), "Provisioned tablet", "1.0.0", "windows", "Initial setup");

        await using var db = fixture.CreateDbContext();
        var service = CreateService(db);
        var first = await service.ProvisionAsync(graph.KioskId, request, "client-device-provision-replay", actor);
        var replay = await service.ProvisionAsync(graph.KioskId, request, "client-device-provision-replay", actor);
        var conflict = await service.ProvisionAsync(
            graph.KioskId,
            request with { DisplayName = "Changed tablet" },
            "client-device-provision-replay",
            actor);

        Assert.True(first.Succeeded, first.Message);
        Assert.True(replay.Succeeded, replay.Message);
        Assert.Equal(first.Data!.Id, replay.Data!.Id);
        Assert.False(conflict.Succeeded);
        Assert.Equal(409, conflict.StatusCode);
    }

    [IntegrationFact]
    public async Task Credential_rotation_rejects_the_prior_secret_and_accepts_only_the_new_secret()
    {
        var graph = await SeedKioskAsync();
        var actor = new CurrentUserContext { AccountId = graph.ActorAccountId, IsSystemAdmin = true };
        var originalCredential = Credential();
        var replacementCredential = Credential();

        await using var db = fixture.CreateDbContext();
        var management = CreateService(db);
        var provisioned = await management.ProvisionAsync(
            graph.KioskId,
            new ProvisionClientDeviceRequest(Guid.NewGuid(), originalCredential, "Rotation tablet", "1.0.0", "windows", "Initial setup"),
            "client-device-rotation-provision",
            actor);
        Assert.True(provisioned.Succeeded, provisioned.Message);

        var sessions = CreateSessionService(db);
        var beforeRotation = await sessions.CreateAsync(new CreateClientDeviceSessionRequest(
            provisioned.Data!.Id,
            provisioned.Data.InstallationId,
            originalCredential,
            "1.0.0",
            "windows"));
        Assert.True(beforeRotation.Succeeded, beforeRotation.Message);

        var rotated = await management.RotateCredentialAsync(
            provisioned.Data.Id,
            new RotateClientDeviceCredentialRequest(
                provisioned.Data.Revision,
                replacementCredential,
                "Replace exposed credential"),
            "client-device-rotation",
            actor);
        Assert.True(rotated.Succeeded, rotated.Message);

        var rejectedPriorSecret = await sessions.CreateAsync(new CreateClientDeviceSessionRequest(
            provisioned.Data.Id,
            provisioned.Data.InstallationId,
            originalCredential,
            "1.0.0",
            "windows"));
        var acceptedReplacementSecret = await sessions.CreateAsync(new CreateClientDeviceSessionRequest(
            provisioned.Data.Id,
            provisioned.Data.InstallationId,
            replacementCredential,
            "1.0.0",
            "windows"));

        Assert.False(rejectedPriorSecret.Succeeded);
        Assert.Equal(401, rejectedPriorSecret.StatusCode);
        Assert.True(acceptedReplacementSecret.Succeeded, acceptedReplacementSecret.Message);
    }

    [IntegrationFact]
    public async Task Scoped_manager_and_technician_cannot_mutate_client_devices_outside_their_scope()
    {
        var source = await SeedKioskAsync();
        var target = await SeedKioskAsync();
        var systemAdmin = new CurrentUserContext { AccountId = source.ActorAccountId, IsSystemAdmin = true };
        var sourceCredential = Credential();

        await using var db = fixture.CreateDbContext();
        var management = CreateService(db);
        var provisioned = await management.ProvisionAsync(
            source.KioskId,
            new ProvisionClientDeviceRequest(Guid.NewGuid(), sourceCredential, "Scoped tablet", "1.0.0", "windows", "Initial setup"),
            "client-device-scope-provision",
            systemAdmin);
        Assert.True(provisioned.Succeeded, provisioned.Message);

        var managerOutsideTarget = new CurrentUserContext
        {
            AccountId = Guid.NewGuid(),
            RoleScopes = [new UserRoleScope("Manager", source.OrganizationId, null, null)]
        };
        var orgAdminInsideSource = new CurrentUserContext
        {
            AccountId = Guid.NewGuid(),
            RoleScopes = [new UserRoleScope("OrgAdmin", source.OrganizationId, null, null)]
        };
        var orgAdminResult = await management.ListAsync(source.KioskId, orgAdminInsideSource);
        var managerResult = await management.ProvisionAsync(
            target.KioskId,
            new ProvisionClientDeviceRequest(Guid.NewGuid(), Credential(), "Out-of-scope tablet", "1.0.0", "windows", "Initial setup"),
            "client-device-manager-outside-scope",
            managerOutsideTarget);

        var technicianSourceOnly = new CurrentUserContext
        {
            AccountId = Guid.NewGuid(),
            RoleScopes = [new UserRoleScope("Technician", null, null, source.KioskId)]
        };
        var technicianResult = await management.RebindAsync(
            provisioned.Data!.Id,
            new RebindClientDeviceRequest(target.KioskId, provisioned.Data.Revision, "Move tablet"),
            "client-device-technician-cross-scope",
            technicianSourceOnly);

        Assert.False(managerResult.Succeeded);
        Assert.Equal(404, managerResult.StatusCode);
        Assert.True(orgAdminResult.Succeeded, orgAdminResult.Message);
        Assert.Contains(orgAdminResult.Data!, device => device.Id == provisioned.Data.Id);
        Assert.False(technicianResult.Succeeded);
        Assert.Equal(404, technicianResult.StatusCode);
    }

    [IntegrationFact]
    public async Task Retire_is_blocked_while_the_kiosk_has_an_active_customer_session()
    {
        var graph = await SeedKioskAsync();
        var actor = new CurrentUserContext { AccountId = graph.ActorAccountId, IsSystemAdmin = true };
        var now = DateTimeOffset.UtcNow;

        await using var db = fixture.CreateDbContext();
        var management = CreateService(db);
        var provisioned = await management.ProvisionAsync(
            graph.KioskId,
            new ProvisionClientDeviceRequest(Guid.NewGuid(), Credential(), "Busy kiosk tablet", "1.0.0", "windows", "Initial setup"),
            "client-device-busy-provision",
            actor);
        Assert.True(provisioned.Succeeded, provisioned.Message);

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Orders" (
                "Id", "OrganizationId", "StoreId", "KioskId", "SourceClientDeviceId", "OrderNumber", "Channel", "Status", "PaymentStatus",
                "Currency", "SubtotalAmount", "DiscountAmount", "TaxAmount", "TotalAmount", "PaidAmount",
                "PlacedAt", "PaymentDeadlineAt", "CreatedAt")
            VALUES (
                {Guid.NewGuid()}, {graph.OrganizationId}, {graph.StoreId}, {graph.KioskId}, {provisioned.Data!.Id}, {"client-device-active-session"},
                {(int)OrderChannel.Tablet}, {(int)OrderStatus.PendingPayment}, {(int)PaymentStatus.Unpaid},
                {"VND"}, {35000m}, {0m}, {0m}, {35000m}, {0m}, {now}, {now.AddMinutes(15)}, {now});
            """);

        var retired = await management.RetireAsync(
            provisioned.Data.Id,
            new ClientDeviceLifecycleRequest(provisioned.Data.Revision, "Retire unavailable tablet"),
            "client-device-busy-retire",
            actor);

        Assert.False(retired.Succeeded);
        Assert.Equal(409, retired.StatusCode);
    }

    private async Task<SeededKiosk> SeedKioskAsync()
    {
        await using var db = fixture.CreateDbContext();
        var organization = new Organization
        {
            Code = $"CLIENT-POSTGRES-{Guid.NewGuid():N}",
            Name = "Client-device integration organization",
            Status = EntityStatus.Active
        };
        var store = new Store
        {
            OrganizationId = organization.Id,
            Code = $"STORE-POSTGRES-{Guid.NewGuid():N}",
            Name = "Client-device integration store",
            Status = EntityStatus.Active
        };
        var kiosk = new Kiosk
        {
            OrganizationId = organization.Id,
            StoreId = store.Id,
            Code = $"KIOSK-POSTGRES-{Guid.NewGuid():N}",
            Name = "Client-device integration kiosk",
            Status = KioskStatus.Active
        };
        var actor = new Account
        {
            UserName = $"client-device-actor-{Guid.NewGuid():N}",
            Email = $"client-device-actor-{Guid.NewGuid():N}@icebot.test",
            Status = AccountStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.AddRange(organization, store, kiosk, actor);
        await db.SaveChangesAsync();
        return new SeededKiosk(organization.Id, store.Id, kiosk.Id, actor.Id);
    }

    private static ClientDeviceManagementService CreateService(IceBotDbContext db) =>
        new(
            new ClientDeviceStore(db),
            CreateCredentialHasher());

    private static ClientDeviceSessionService CreateSessionService(IceBotDbContext db) =>
        new(
            new ClientDeviceStore(db),
            CreateCredentialHasher(),
            new TestClientDeviceTokenIssuer(),
            Options.Create(SecurityOptions()),
            NullLogger<ClientDeviceSessionService>.Instance);

    private static ClientDeviceCredentialHasher CreateCredentialHasher() =>
        new(Options.Create(SecurityOptions()));

    private static ClientDeviceSecurityOptions SecurityOptions() => new()
    {
        CurrentHashKeyVersion = "integration-v1",
        HashKeys = new Dictionary<string, string> { ["integration-v1"] = "integration-client-device-hash-key" },
        JwtSecret = "integration-client-device-jwt-secret-must-be-long-enough",
        Issuer = "IceBot.Integration.ClientDevice",
        Audience = "IceBot.Integration.Runtime"
    };

    private static string Credential() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private sealed class TestClientDeviceTokenIssuer : IClientDeviceTokenIssuer
    {
        public string Issue(Guid clientDeviceId, Guid kioskId, int credentialVersion, int sessionVersion) =>
            $"test:{clientDeviceId:N}:{kioskId:N}:{credentialVersion}:{sessionVersion}";
    }

    private sealed record SeededKiosk(Guid OrganizationId, Guid StoreId, Guid KioskId, Guid ActorAccountId);
}
