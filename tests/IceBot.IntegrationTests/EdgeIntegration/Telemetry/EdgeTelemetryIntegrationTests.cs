using Application.RobotConfiguration.Programs.Commands;
using Infrastructure.Concurrency;
using Infrastructure.RobotConfiguration.Storage.ObjectStorage;
using Application.RobotConfiguration.Storage.Abstractions;
using Domain.Sync.Ingestion;
using Domain.Devices.Telemetry;
using Domain.Devices.Connectivity;
using Domain.Devices.ExecutionEndpoints;
using System.Text;
using System.Text.Json;
using Application.EdgeIntegration;
using Application.EdgeIntegration.Dispatch;
using Application.EdgeIntegration.Reports;
using Application.EdgeIntegration.CommandDelivery.Commands;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.EdgeIntegration.Reports.Commands;
using Application.EdgeIntegration.Timeouts.Commands;
using Application.EdgeIntegration.CommandDelivery.Services;
using Application.EdgeIntegration.Dispatch.Services;
using Application.EdgeIntegration.Reports.Services;
using Application.Devices.Telemetry;
using Application.Devices.Catalog.Commands;
using Application.Devices.ExecutionEndpoints.Commands;
using Application.Devices.Telemetry.Commands;
using Application.Devices.Connectivity.Commands;
using Application.Devices.Credentials.Commands;
using Application.Operations.Alerts.Notifications;
using Application.Identity.Tokens.Claims;
using Application.Orders.Management.Queries;
using Application.Orders.Management.Commands;
using Application.Orders.PlaceOrder.Queries;
using Application.ProductionConfiguration.Releases.Commands;
using Application.ProductionConfiguration.Deployments.Commands;
using Application.ProductionConfiguration.Routes.Commands;
using Application.ProductionConfiguration.Releases.Services;
using Application.ProductionConfiguration.Readiness.Services;
using Application.ProductionConfiguration;
using Application.ProductionConfiguration.Deployments;
using Application.ProductionConfiguration.Readiness;
using Application.ProductionPackages.Ownership;
using Application.Inventory.Services;
using Application.Inventory.Commands;
using Application.RobotConfiguration.Artifacts.Commands;
using Application.RobotConfiguration.Storage.Services;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;
using Domain.Common.Enums;
using Domain.Devices.Catalog;
using Domain.Identity.Entities;
using Domain.Inventory.Entities;
using Domain.Inventory.Enums;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.Orders.Incidents;
using Domain.Operations.Enums;
using Domain.Operations.Entities;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;
using Domain.ProductionExecution.Enums;
using Domain.SalesCatalog.Entities;
using Domain.SalesCatalog.Enums;
using Domain.Sync.Enums;
using Domain.Sync.Entities;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.EdgeIntegration.Persistence;
using Infrastructure.Devices.Catalog.Persistence;
using Infrastructure.Devices.Connectivity.Persistence;
using Infrastructure.Devices.ExecutionEndpoints.Persistence;
using Infrastructure.Devices.Telemetry.Persistence;
using Infrastructure.Orders.Persistence;
using Infrastructure.Inventory.Persistence;
using Infrastructure.ProductionConfiguration.Persistence.Deployments;
using Infrastructure.ProductionConfiguration.Persistence.Releases;
using Infrastructure.ProductionConfiguration.Persistence.Routes;
using Infrastructure.ProductionPackages;
using Infrastructure.RobotConfiguration.Artifacts.Persistence;
using Infrastructure.RobotConfiguration.ArtifactContracts;
using Infrastructure.RobotConfiguration.Programs.Persistence;
using Infrastructure.Persistence.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Domain.Devices.ExecutionEndpoints.Projections;
using Domain.RobotConfiguration.ArtifactContracts;

namespace IceBot.IntegrationTests.EdgeIntegration;


[Collection(IntegrationTestFixture.CollectionName)]
public sealed class EdgeTelemetryIntegrationTests(IntegrationTestFixture fixture)
    : EdgeOperationalIntegrationTestBase(fixture)
{
    [IntegrationFact]
    public async Task ReadinessIngestion_AppliesNewRevisionAndReplacesCapabilitySnapshot()
    {
        var graph = await SeedPrerequisitesAsync();
        var publisher = new NoOpRealtimeNotificationPublisher();

        async Task<Application.Shared.Wrappers.ApiResult<Application.Devices.Connectivity.Results.ExecutionReadinessResult>> IngestAsync(
            long revision,
            params ExecutionCapabilityInput[] capabilities)
        {
            await using var dbContext = _fixture.CreateDbContext();
            return await new IngestExecutionReadinessCommandHandler(
                new ExecutionReadinessStore(dbContext),
                publisher,
                Options.Create(new EdgeTelemetryIngestionOptions()))
                .HandleAsync(new IngestExecutionReadinessCommand
                {
                    KioskId = graph.KioskId,
                    EndpointId = graph.EndpointId,
                    SourceExecutorId = graph.SourceExecutorId,
                    StateRevision = revision,
                    ExecutorReportedAt = DateTimeOffset.UtcNow.AddSeconds(-1),
                    Readiness = ExecutionReadinessState.Ready,
                    Activity = ExecutionActivityState.Idle,
                    Safety = ExecutionSafetyState.Safe,
                    LocalPersistenceHealth = HealthyLocalPersistence(),
                    Capabilities = capabilities
                });
        }

        var revisionTwo = new ExecutionCapabilityInput("ICE_CREAM", "CELL-A", true, null);
        var first = await IngestAsync(2, revisionTwo);
        var duplicate = await IngestAsync(2, revisionTwo);
        var replacement = await IngestAsync(3,
            new ExecutionCapabilityInput("COFFEE", "CELL-B", false, "Cleaning"));

        Assert.True(first.Succeeded, first.Message);
        Assert.True(first.Data!.Applied);
        Assert.True(duplicate.Succeeded, duplicate.Message);
        Assert.True(duplicate.Data!.DuplicateOrStale);
        Assert.True(replacement.Succeeded, replacement.Message);
        Assert.Equal(2, publisher.ExecutionReadinessChangedEvents.Count);

        await using var assertionContext = _fixture.CreateDbContext();
        var projection = await assertionContext.ExecutionEndpointReadinessProjections
            .Include(item => item.Capabilities)
            .SingleAsync(item => item.KioskExecutionEndpointId == graph.EndpointId);
        Assert.Equal(3, projection.StateRevision);
        var capability = Assert.Single(projection.Capabilities);
        Assert.Equal("COFFEE", capability.CapabilityCode);
        Assert.False(capability.IsAvailable);
        Assert.Equal("Cleaning", capability.UnavailableReason);
    }

    [IntegrationFact]
    public async Task HeartbeatIngestion_DeduplicatesAndDoesNotLetStaleSequenceRewindConnectivity()
    {
        var graph = await SeedPrerequisitesAsync();
        var reportedAt = DateTimeOffset.UtcNow.AddSeconds(-5);
        var publisher = new NoOpRealtimeNotificationPublisher();

        async Task<Application.Shared.Wrappers.ApiResult<Application.Devices.Telemetry.Results.HeartbeatIngestResult>> IngestAsync(
            long sequence,
            KioskHeartbeatStatus status)
        {
            await using var dbContext = _fixture.CreateDbContext();
            return await new IngestKioskHeartbeatCommandHandler(
                new EdgeTelemetryIngestionStore(dbContext),
                publisher,
                Options.Create(new EdgeTelemetryIngestionOptions()))
                .HandleAsync(new IngestKioskHeartbeatCommand
                {
                    KioskId = graph.KioskId,
                    EndpointId = graph.EndpointId,
                    OriginNodeId = graph.SourceExecutorId,
                    HeartbeatSequence = sequence,
                    ReportedAt = reportedAt,
                    Status = status,
                    AppVersion = "1.0.0",
                    CpuUsagePercent = 10,
                    MemoryUsagePercent = 20,
                    DiskUsagePercent = 30
                });
        }

        var first = await IngestAsync(2, KioskHeartbeatStatus.Online);
        var stale = await IngestAsync(1, KioskHeartbeatStatus.Offline);
        var duplicate = await IngestAsync(2, KioskHeartbeatStatus.Online);

        Assert.True(first.Succeeded, first.Message);
        Assert.Equal(201, first.StatusCode);
        Assert.False(first.Data!.Duplicate);
        Assert.True(stale.Succeeded, stale.Message);
        Assert.True(stale.Data!.Stale);
        Assert.True(duplicate.Succeeded, duplicate.Message);
        Assert.True(duplicate.Data!.Duplicate);
        Assert.Equal(first.Data.HeartbeatId, duplicate.Data.HeartbeatId);

        await using var assertionContext = _fixture.CreateDbContext();
        var heartbeats = await assertionContext.KioskHeartbeats
            .Where(item =>
                item.KioskId == graph.KioskId &&
                item.NodeId == graph.SourceExecutorId)
            .ToListAsync();
        Assert.Equal(2, heartbeats.Count);
        var heartbeat = Assert.Single(heartbeats, item => item.HeartbeatSequence == 2);
        var kiosk = await assertionContext.Kiosks.SingleAsync(item => item.Id == graph.KioskId);
        AssertPostgresTimestampEqual(first.Data.ReceivedAt, heartbeat.ReceivedAt);
        AssertPostgresTimestampEqual(first.Data.ReceivedAt, kiosk.LastOnlineAt!.Value);
        Assert.Equal(KioskStatus.Active, kiosk.Status);
        var connectivity = await assertionContext.KioskConnectivityProjections
            .SingleAsync(item => item.KioskId == graph.KioskId);
        Assert.Equal(KioskConnectivityStatus.Online, connectivity.Status);
        var statusEvent = Assert.Single(publisher.KioskStatusChangedEvents);
        Assert.Equal(KioskConnectivityStatus.Unknown.ToString(), statusEvent.OldConnectivity);
        Assert.Equal(KioskConnectivityStatus.Online.ToString(), statusEvent.NewConnectivity);
        Assert.Equal("HeartbeatConnectivityChanged", statusEvent.Reason);
    }

    [IntegrationFact]
    public async Task ConnectivityReconciliation_TransitionsTimedOutActiveKioskOnce()
    {
        var graph = await SeedPrerequisitesAsync();
        var observedAt = DateTimeOffset.UtcNow;
        var publisher = new NoOpRealtimeNotificationPublisher();

        await using (var setupContext = _fixture.CreateDbContext())
        {
            var setupKiosk = await setupContext.Kiosks.SingleAsync(item => item.Id == graph.KioskId);
            setupKiosk.Status = KioskStatus.Active;
            setupKiosk.LastOnlineAt = observedAt.AddMinutes(-5);
            var setupConnectivity = KioskConnectivityProjection.Create(graph.KioskId, observedAt.AddMinutes(-10));
            setupConnectivity.Observe(
                KioskConnectivityStatus.Online,
                graph.SourceExecutorId,
                1,
                observedAt.AddMinutes(-5));
            setupContext.KioskConnectivityProjections.Add(setupConnectivity);
            await setupContext.SaveChangesAsync();
        }

        async Task<bool> ReconcileAsync()
        {
            await using var dbContext = _fixture.CreateDbContext();
            return await new ReconcileKioskConnectivityCommandHandler(
                new EdgeTelemetryIngestionStore(dbContext),
                publisher,
                Options.Create(new EdgeTelemetryIngestionOptions { HeartbeatTimeoutSeconds = 90 }))
                .HandleAsync(new ReconcileKioskConnectivityCommand
                {
                    KioskId = graph.KioskId,
                    ObservedAt = observedAt
                });
        }

        Assert.True(await ReconcileAsync());
        Assert.False(await ReconcileAsync());

        await using var assertionContext = _fixture.CreateDbContext();
        var kiosk = await assertionContext.Kiosks.SingleAsync(item => item.Id == graph.KioskId);
        Assert.Equal(KioskStatus.Active, kiosk.Status);
        var connectivity = await assertionContext.KioskConnectivityProjections
            .SingleAsync(item => item.KioskId == graph.KioskId);
        Assert.Equal(KioskConnectivityStatus.Unreachable, connectivity.Status);
        var statusEvent = Assert.Single(publisher.KioskStatusChangedEvents);
        Assert.Equal(KioskConnectivityStatus.Online.ToString(), statusEvent.OldConnectivity);
        Assert.Equal(KioskConnectivityStatus.Unreachable.ToString(), statusEvent.NewConnectivity);
        Assert.Equal("HeartbeatTimeout", statusEvent.Reason);
    }

}