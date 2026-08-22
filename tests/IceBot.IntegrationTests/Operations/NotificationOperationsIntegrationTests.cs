using Application.Identity.Tokens.Claims;
using Application.Operations.Notifications.Recovery;
using Domain.Common.Enums;
using Domain.Identity.Entities;
using Domain.Identity.Enums;
using Domain.Operations.Entities;
using Domain.Operations.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Operations.Persistence;
using Infrastructure.ProductionConfiguration.Persistence.Deployments;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace IceBot.IntegrationTests.Operations;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class NotificationOperationsIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task RequeuePermanentFailure_PersistsFreshBudgetAndAudit()
    {
        await using var db = fixture.CreateDbContext();
        var organization = new Organization
        {
            Code = $"REQUEUE-{Guid.NewGuid():N}",
            Name = "Requeue organization",
            Status = EntityStatus.Active
        };
        var store = new Store
        {
            OrganizationId = organization.Id,
            Code = $"STORE-{Guid.NewGuid():N}",
            Name = "Requeue store",
            Status = EntityStatus.Active
        };
        var kiosk = new Kiosk
        {
            OrganizationId = organization.Id,
            StoreId = store.Id,
            Code = $"KIOSK-{Guid.NewGuid():N}",
            Name = "Requeue kiosk",
            Status = KioskStatus.Active
        };
        var actor = new Account
        {
            UserName = $"requeue-{Guid.NewGuid():N}",
            Email = $"requeue-{Guid.NewGuid():N}@example.test",
            Status = AccountStatus.Active
        };
        db.AddRange(organization, store, kiosk, actor);
        await db.SaveChangesAsync();
        var delivery = NotificationDelivery.CreatePush(
            organization.Id, store.Id, kiosk.Id, Guid.NewGuid(), $"requeue:{Guid.NewGuid():N}",
            "test", actor.Id, "title", "body", "{}", DateTimeOffset.UtcNow, 1);
        delivery.MarkProcessing(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        delivery.MarkFailed("FAILED", "transport failed", DateTimeOffset.UtcNow);
        db.NotificationDeliveries.Add(delivery);
        await db.SaveChangesAsync();

        var service = new RequeueNotificationDeliveryService(
            new NotificationDeliveryReadStore(db), new NotificationDeliveryStore(db), new OperationLogStore(db));
        var result = await service.RequeueAsync(new CurrentUserContext
        {
            AccountId = actor.Id,
            IsSystemAdmin = true
        }, organization.Id, delivery.Id, "Operator confirmed transport recovery.");

        Assert.True(result.Succeeded);
        db.ChangeTracker.Clear();
        var persisted = await db.NotificationDeliveries.SingleAsync(x => x.Id == delivery.Id);
        Assert.Equal(NotificationDeliveryStatus.Pending, persisted.Status);
        Assert.Equal(0, persisted.AttemptCount);
        Assert.True(await db.OperationLogs.AnyAsync(x =>
            x.CorrelationId == delivery.Id && x.Action == "NotificationDeliveryRequeued"));
    }

    [IntegrationFact]
    public async Task DeploymentFailureCandidateQuery_TranslatesAgainstPostgreSql()
    {
        await using var db = fixture.CreateDbContext();
        var rows = await new DeploymentFailureNotificationStore(db).ListPendingIdsAsync(10);
        Assert.NotNull(rows);
    }

    [IntegrationFact]
    public async Task RequeueHttpContract_EnforcesTenantAndTerminalState()
    {
        var actorId = Guid.NewGuid();
        var ownOrganization = new Organization
        {
            Code = $"REQUEUE-OWN-{Guid.NewGuid():N}",
            Name = "Own organization",
            Status = EntityStatus.Active
        };
        var otherOrganization = new Organization
        {
            Code = $"REQUEUE-OTHER-{Guid.NewGuid():N}",
            Name = "Other organization",
            Status = EntityStatus.Active
        };
        var ownStore = CreateStore(ownOrganization.Id, "OWN");
        var otherStore = CreateStore(otherOrganization.Id, "OTHER");
        var ownKiosk = CreateKiosk(ownOrganization.Id, ownStore.Id, "OWN");
        var otherKiosk = CreateKiosk(otherOrganization.Id, otherStore.Id, "OTHER");
        var actor = new Account
        {
            Id = actorId,
            UserName = $"requeue-http-{Guid.NewGuid():N}",
            Email = $"requeue-http-{Guid.NewGuid():N}@example.test",
            Status = AccountStatus.Active
        };
        var pending = NotificationDelivery.CreatePush(
            ownOrganization.Id, ownStore.Id, ownKiosk.Id, Guid.NewGuid(), $"pending:{Guid.NewGuid():N}",
            "test", actorId, "title", "body", "{}", DateTimeOffset.UtcNow);
        var otherFailed = NotificationDelivery.CreatePush(
            otherOrganization.Id, otherStore.Id, otherKiosk.Id, Guid.NewGuid(), $"failed:{Guid.NewGuid():N}",
            "test", actorId, "title", "body", "{}", DateTimeOffset.UtcNow, 1);
        otherFailed.MarkProcessing(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        otherFailed.MarkFailed("FAILED", "failed", DateTimeOffset.UtcNow);
        await using (var seed = fixture.CreateDbContext())
        {
            seed.AddRange(ownOrganization, otherOrganization, ownStore, otherStore, ownKiosk, otherKiosk, actor,
                pending, otherFailed);
            await seed.SaveChangesAsync();
        }

        await using var factory = new PackageApiWebApplicationFactory(
            fixture,
            fixture.CreateObjectStorage(autoCreateBucket: true),
            actorId,
            "OrgAdmin",
            [$"OrgAdmin|{ownOrganization.Id:D}|*|*"]);
        using var client = factory.CreateAuthenticatedClient();
        var body = JsonContent.Create(new { reason = "Operator confirmed provider recovery." });

        using var crossTenant = await client.PostAsync(
            $"/api/v1/management/organizations/{otherOrganization.Id:D}/notification-deliveries/{otherFailed.Id:D}/requeue",
            body);
        Assert.Equal(HttpStatusCode.NotFound, crossTenant.StatusCode);

        using var nonTerminal = await client.PostAsJsonAsync(
            $"/api/v1/management/organizations/{ownOrganization.Id:D}/notification-deliveries/{pending.Id:D}/requeue",
            new { reason = "Operator requested an invalid early retry." });
        Assert.Equal(HttpStatusCode.Conflict, nonTerminal.StatusCode);
    }

    private static Store CreateStore(Guid organizationId, string suffix) => new()
    {
        OrganizationId = organizationId,
        Code = $"STORE-{suffix}-{Guid.NewGuid():N}",
        Name = $"Store {suffix}",
        Status = EntityStatus.Active
    };

    private static Kiosk CreateKiosk(Guid organizationId, Guid storeId, string suffix) => new()
    {
        OrganizationId = organizationId,
        StoreId = storeId,
        Code = $"KIOSK-{suffix}-{Guid.NewGuid():N}",
        Name = $"Kiosk {suffix}",
        Status = KioskStatus.Active
    };
}
