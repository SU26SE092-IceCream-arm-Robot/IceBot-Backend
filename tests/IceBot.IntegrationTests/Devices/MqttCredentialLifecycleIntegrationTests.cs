using Application.Devices.Credentials.Abstractions;
using Application.Devices.Credentials.Commands;
using Domain.Devices.ExecutionEndpoints;
using Domain.Tenants.Entities;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.Devices.ExecutionEndpoints.Persistence;
using Infrastructure.Operations.Persistence;
using Application.Operations.Alerts.Automation;
using Domain.Operations.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace IceBot.IntegrationTests.Devices;

[Collection(IntegrationTestFixture.CollectionName)]
public sealed class MqttCredentialLifecycleIntegrationTests(IntegrationTestFixture fixture)
{
    [IntegrationFact]
    public async Task OperationalAlert_IsPersistedDeduplicatedAndResolvedAfterCredentialRecovery()
    {
        var graph = await SeedEndpointAsync();
        await SeedStaleCredentialAsync(graph.EndpointId, pendingRevoke: false);
        var observedAt = DateTimeOffset.UtcNow;
        await using (var credentialDb = fixture.CreateDbContext())
        {
            var outcome = await new ReconcileStaleMqttEndpointCredentialCommandHandler(
                    new ExecutionEndpointStore(credentialDb),
                    new ObservingProvisioner(() => Task.CompletedTask))
                .HandleAsync(new ReconcileStaleMqttEndpointCredentialCommand(
                    graph.EndpointId, observedAt));
            Assert.Equal(MqttCredentialReconciliationOutcome.ProvisioningMarkedFailed, outcome);
        }

        await ReconcileAlertAsync(
            graph.EndpointId,
            MqttCredentialReconciliationOutcome.ProvisioningMarkedFailed,
            observedAt);
        await using (var activeAlertDb = fixture.CreateDbContext())
        {
            var activeAlertIds = await new MqttCredentialAlertAutomationStore(activeAlertDb)
                .ListActiveAlertEndpointIdsAsync(10);
            Assert.Contains(graph.EndpointId, activeAlertIds);
        }
        await ReconcileAlertAsync(graph.EndpointId, null, observedAt.AddMinutes(1));

        await using (var recoveryDb = fixture.CreateDbContext())
        {
            var credential = await recoveryDb.ExecutionEndpointMqttCredentials
                .SingleAsync(value => value.KioskExecutionEndpointId == graph.EndpointId);
            credential.RetryFailedProvisionOrRotation();
            credential.MarkActive(observedAt.AddMinutes(2));
            await recoveryDb.SaveChangesAsync();
        }
        await ReconcileAlertAsync(graph.EndpointId, null, observedAt.AddMinutes(2));

        await using var assertionContext = fixture.CreateDbContext();
        var alerts = await assertionContext.Alerts.AsNoTracking()
            .Where(alert =>
                alert.SourceType == "ExecutionEndpointMqttCredential" &&
                alert.SourceId == graph.EndpointId)
            .ToListAsync();
        var alert = Assert.Single(alerts);
        Assert.Equal(MqttCredentialOperationalAlertReconciler.TimeoutCode, alert.AlertCode);
        Assert.Equal(1, alert.OccurrenceCount);
        Assert.Equal(AlertStatus.Resolved, alert.Status);
    }

    [IntegrationFact]
    public async Task Reconciliation_StaleProvision_IsSelectedAndMarkedFailed()
    {
        var graph = await SeedEndpointAsync();
        await SeedStaleCredentialAsync(graph.EndpointId, pendingRevoke: false);
        await using var db = fixture.CreateDbContext();
        var store = new ExecutionEndpointStore(db);
        var candidates = await store.ListStaleMqttCredentialEndpointIdsAsync(
            DateTimeOffset.UtcNow.AddMinutes(-5), 10);
        var handler = new ReconcileStaleMqttEndpointCredentialCommandHandler(
            store, new ObservingProvisioner(() => Task.CompletedTask));

        var outcome = await handler.HandleAsync(
            new ReconcileStaleMqttEndpointCredentialCommand(
                graph.EndpointId, DateTimeOffset.UtcNow));

        Assert.Contains(graph.EndpointId, candidates);
        Assert.Equal(MqttCredentialReconciliationOutcome.ProvisioningMarkedFailed, outcome);
        await using var assertionContext = fixture.CreateDbContext();
        var credential = await assertionContext.ExecutionEndpointMqttCredentials.AsNoTracking()
            .SingleAsync(value => value.KioskExecutionEndpointId == graph.EndpointId);
        Assert.Equal(ExecutionEndpointMqttCredentialStatus.Failed, credential.Status);
    }

    [IntegrationFact]
    public async Task Reconciliation_StaleRevoke_IsClaimedAndCompleted()
    {
        var graph = await SeedEndpointAsync();
        await SeedStaleCredentialAsync(graph.EndpointId, pendingRevoke: true);
        await using var db = fixture.CreateDbContext();
        var handler = new ReconcileStaleMqttEndpointCredentialCommandHandler(
            new ExecutionEndpointStore(db),
            new ObservingProvisioner(() => Task.CompletedTask));

        var outcome = await handler.HandleAsync(
            new ReconcileStaleMqttEndpointCredentialCommand(
                graph.EndpointId, DateTimeOffset.UtcNow));

        Assert.Equal(MqttCredentialReconciliationOutcome.Revoked, outcome);
        await using var assertionContext = fixture.CreateDbContext();
        var credential = await assertionContext.ExecutionEndpointMqttCredentials.AsNoTracking()
            .SingleAsync(value => value.KioskExecutionEndpointId == graph.EndpointId);
        Assert.Equal(ExecutionEndpointMqttCredentialStatus.Revoked, credential.Status);
        Assert.Equal(3, credential.CredentialVersion);
    }

    [IntegrationFact]
    public async Task Provision_CommitsPendingIntentBeforeBrokerIo_ThenActivates()
    {
        var graph = await SeedEndpointAsync();
        var provisioner = new ObservingProvisioner(async () =>
        {
            await using var observer = fixture.CreateDbContext();
            var pending = await observer.ExecutionEndpointMqttCredentials.AsNoTracking()
                .SingleAsync(value => value.KioskExecutionEndpointId == graph.EndpointId);
            Assert.Equal(ExecutionEndpointMqttCredentialStatus.PendingProvision, pending.Status);
        });
        await using var db = fixture.CreateDbContext();
        var handler = new ProvisionMqttEndpointCredentialCommandHandler(
            new ExecutionEndpointStore(db), provisioner);

        var result = await handler.HandleAsync(new ProvisionMqttEndpointCredentialCommand
        {
            EndpointId = graph.EndpointId,
            KioskId = graph.KioskId,
            UserContext = new() { AccountId = Guid.NewGuid(), IsSystemAdmin = true }
        });

        Assert.True(result.Succeeded);
        await using var assertionContext = fixture.CreateDbContext();
        var active = await assertionContext.ExecutionEndpointMqttCredentials.AsNoTracking()
            .SingleAsync(value => value.KioskExecutionEndpointId == graph.EndpointId);
        Assert.Equal(ExecutionEndpointMqttCredentialStatus.Active, active.Status);
    }

    [IntegrationFact]
    public async Task Provision_CallerCancellationAfterPreparation_LeavesRecoverablePendingIntent()
    {
        var graph = await SeedEndpointAsync();
        using var cancellation = new CancellationTokenSource();
        var provisioner = new ObservingProvisioner(() =>
        {
            cancellation.Cancel();
            throw new OperationCanceledException(cancellation.Token);
        });
        await using var db = fixture.CreateDbContext();
        var handler = new ProvisionMqttEndpointCredentialCommandHandler(
            new ExecutionEndpointStore(db), provisioner);

        await Assert.ThrowsAsync<OperationCanceledException>(() => handler.HandleAsync(
            new ProvisionMqttEndpointCredentialCommand
            {
                EndpointId = graph.EndpointId,
                KioskId = graph.KioskId,
                UserContext = new() { AccountId = Guid.NewGuid(), IsSystemAdmin = true }
            }, cancellation.Token));

        await using var assertionContext = fixture.CreateDbContext();
        var pending = await assertionContext.ExecutionEndpointMqttCredentials.AsNoTracking()
            .SingleAsync(value => value.KioskExecutionEndpointId == graph.EndpointId);
        Assert.Equal(ExecutionEndpointMqttCredentialStatus.PendingProvision, pending.Status);
    }

    [IntegrationFact]
    public async Task Provision_WhenOperationIsSuperseded_DoesNotOverwriteNewerVersion()
    {
        var graph = await SeedEndpointAsync();
        var provisioner = new ObservingProvisioner(async () =>
        {
            await using var concurrent = fixture.CreateDbContext();
            await concurrent.ExecutionEndpointMqttCredentials
                .Where(value => value.KioskExecutionEndpointId == graph.EndpointId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(value => value.CredentialVersion, value => value.CredentialVersion + 1)
                    .SetProperty(value => value.UpdatedAt, DateTimeOffset.UtcNow));
        });
        await using var db = fixture.CreateDbContext();
        var handler = new ProvisionMqttEndpointCredentialCommandHandler(
            new ExecutionEndpointStore(db), provisioner);

        var result = await handler.HandleAsync(new ProvisionMqttEndpointCredentialCommand
        {
            EndpointId = graph.EndpointId,
            KioskId = graph.KioskId,
            UserContext = new() { AccountId = Guid.NewGuid(), IsSystemAdmin = true }
        });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        await using var assertionContext = fixture.CreateDbContext();
        var superseding = await assertionContext.ExecutionEndpointMqttCredentials.AsNoTracking()
            .SingleAsync(value => value.KioskExecutionEndpointId == graph.EndpointId);
        Assert.Equal(2, superseding.CredentialVersion);
        Assert.Equal(ExecutionEndpointMqttCredentialStatus.PendingProvision, superseding.Status);
    }

    private async Task<(Guid KioskId, Guid EndpointId)> SeedEndpointAsync()
    {
        await using var db = fixture.CreateDbContext();
        var organization = new Organization
        {
            Code = $"ORG-{Guid.NewGuid():N}",
            Name = "MQTT lifecycle organization"
        };
        var store = new Store
        {
            OrganizationId = organization.Id,
            Code = $"STORE-{Guid.NewGuid():N}",
            Name = "MQTT lifecycle store",
            TimeZone = "Asia/Bangkok"
        };
        var kiosk = new Kiosk
        {
            OrganizationId = organization.Id,
            StoreId = store.Id,
            Code = $"KIOSK-{Guid.NewGuid():N}",
            Name = "MQTT lifecycle kiosk"
        };
        var endpoint = KioskExecutionEndpoint.CreateProvisioning(
            kiosk.Id,
            $"EDGE-{Guid.NewGuid():N}",
            KioskExecutionProfile.FullEdge,
            ExecutionEndpointAuthenticationMode.MutualTls);
        db.AddRange(organization, store, kiosk, endpoint);
        await db.SaveChangesAsync();
        return (kiosk.Id, endpoint.Id);
    }

    private async Task SeedStaleCredentialAsync(Guid endpointId, bool pendingRevoke)
    {
        await using var db = fixture.CreateDbContext();
        var credential = ExecutionEndpointMqttCredential.BeginProvision(
            endpointId, "IntegrationTest");
        credential.CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        if (pendingRevoke)
        {
            credential.MarkActive(DateTimeOffset.UtcNow.AddMinutes(-10));
            credential.BeginRevocation();
        }
        db.Add(credential);
        await db.SaveChangesAsync();
    }

    private async Task ReconcileAlertAsync(
        Guid endpointId,
        MqttCredentialReconciliationOutcome? occurrence,
        DateTimeOffset observedAt)
    {
        await using var db = fixture.CreateDbContext();
        var reconciler = new MqttCredentialOperationalAlertReconciler(
            new MqttCredentialAlertAutomationStore(db),
            new NoOpRealtimeNotificationPublisher(),
            NullLogger<MqttCredentialOperationalAlertReconciler>.Instance);
        await reconciler.ReconcileAsync(endpointId, occurrence, observedAt);
    }

    private sealed class ObservingProvisioner(Func<Task> onProvision) : IMqttEndpointCredentialProvisioner
    {
        public string ProviderName => "IntegrationTest";

        public string GetSubscribeTopic(Guid endpointId) =>
            $"icebot/execution-endpoints/{endpointId:D}/commands/available";

        public string GetUplinkPublishTopicPattern(Guid endpointId) =>
            $"icebot/execution-endpoints/{endpointId:D}/uplink/{{messageType}}";

        public string GetUplinkResultTopic(Guid endpointId) =>
            $"icebot/execution-endpoints/{endpointId:D}/uplink/results";

        public async Task ProvisionOrReplaceAsync(
            Guid endpointId,
            string username,
            string password,
            CancellationToken cancellationToken = default) =>
            await onProvision();

        public Task RevokeAsync(
            Guid endpointId,
            string username,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
