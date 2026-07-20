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
public sealed class RobotArtifactOperationalSmokeTests
{
    private const string RuntimeTargetCode = "FAIRINO_LUA_V1";
    private const string MachineModelCode = "FR5";
    private readonly IntegrationTestFixture _fixture;

    public RobotArtifactOperationalSmokeTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [IntegrationFact]
    public async Task ConcurrentAcceptedAcknowledgements_CreateOneExecutionProjectionAndOneOrderTransition()
    {
        var graph = await SeedPrerequisitesAsync();
        var orderId = await CreatePaidOrderAsync(graph);
        Guid commandId;
        var acknowledgedAt = DateTimeOffset.UtcNow;
        await using (var setup = _fixture.CreateDbContext())
        {
            var release = ConfigurationRelease.CreateDraft(graph.OrganizationId, 1);
            setup.ConfigurationReleases.Add(release);
            var command = EdgeCommand.Create(
                EdgeCommandType.ExecuteOrder,
                graph.KioskId,
                graph.EndpointId,
                JsonSerializer.Serialize(new
                {
                    SchemaVersion = 2,
                    ConfigurationReleaseId = release.Id,
                    ReleaseChecksum = "concurrent-ack-release"
                }),
                acknowledgedAt.AddSeconds(-2),
                orderId,
                dispatchAttemptNo: 1,
                commandExpiryAt: acknowledgedAt.AddMinutes(5));
            command.RecordDeliveryAttempt(1, acknowledgedAt.AddSeconds(-1), EdgeCommandDeliveryOutcome.Sent);
            setup.EdgeCommands.Add(command);
            await setup.SaveChangesAsync();
            commandId = command.Id;
        }

        async Task<Application.Shared.Wrappers.ApiResult<Application.EdgeIntegration.CommandDelivery.Results.EdgeCommandAckResult>>
            AcknowledgeAsync()
        {
            await using var db = _fixture.CreateDbContext();
            return await new AcknowledgeEdgeCommandCommandHandler(
                new EdgeCommandStore(db),
                new NoOpRealtimeNotificationPublisher())
                .HandleAsync(new AcknowledgeEdgeCommandCommand
                {
                    KioskId = graph.KioskId,
                    EndpointId = graph.EndpointId,
                    CommandId = commandId,
                    AckStatus = "Accepted",
                    AcknowledgedAt = acknowledgedAt
                });
        }

        var results = await Task.WhenAll(AcknowledgeAsync(), AcknowledgeAsync());

        Assert.All(results, result => Assert.True(result.Succeeded, result.Message));
        await using var assertion = _fixture.CreateDbContext();
        Assert.Equal(1, await assertion.OrderExecutionRecords.CountAsync(record =>
            record.SourceCommandId == commandId));
        Assert.Equal(1, await assertion.OrderStatusHistories.CountAsync(history =>
            history.OrderId == orderId && history.ToStatus == OrderStatus.Accepted));
        Assert.Equal(OrderStatus.Accepted, await assertion.Orders
            .Where(order => order.Id == orderId)
            .Select(order => order.Status)
            .SingleAsync());
    }

    [IntegrationFact]
    public async Task ConcurrentCommandPulls_RecordDistinctAttemptsForTheSameCommand()
    {
        var graph = await SeedPrerequisitesAsync();
        var orderId = await CreatePaidOrderAsync(graph);
        Guid commandId;
        await using (var setup = _fixture.CreateDbContext())
        {
            var now = DateTimeOffset.UtcNow;
            var command = EdgeCommand.Create(
                EdgeCommandType.ExecuteOrder,
                graph.KioskId,
                graph.EndpointId,
                "{}",
                now,
                orderId,
                dispatchAttemptNo: 1,
                commandExpiryAt: now.AddMinutes(5));
            setup.EdgeCommands.Add(command);
            await setup.SaveChangesAsync();
            commandId = command.Id;
        }

        async Task<Application.Shared.Wrappers.ApiResult<Application.EdgeIntegration.CommandDelivery.Results.EdgeCommandPullResult>>
            PullAsync()
        {
            await using var db = _fixture.CreateDbContext();
            return await new PullEdgeCommandsCommandHandler(
                new EdgeCommandStore(db),
                new ArtifactCommandPayloadEnricher(_fixture.CreateObjectStorage()))
                .HandleAsync(new PullEdgeCommandsCommand
                {
                    KioskId = graph.KioskId,
                    EndpointId = graph.EndpointId,
                    MaxCommands = 10
                });
        }

        var results = await Task.WhenAll(PullAsync(), PullAsync());

        Assert.All(results, result => Assert.True(result.Succeeded, result.Message));
        Assert.All(results, result => Assert.Contains(result.Data!.Commands, command => command.CommandId == commandId));
        await using var assertion = _fixture.CreateDbContext();
        var attempts = await assertion.EdgeCommandDeliveryAttempts.AsNoTracking()
            .Where(attempt => attempt.EdgeCommandId == commandId)
            .OrderBy(attempt => attempt.DeliveryAttemptNo)
            .Select(attempt => attempt.DeliveryAttemptNo)
            .ToArrayAsync();
        Assert.Equal([1, 2], attempts);
    }

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

    [IntegrationFact]
    public async Task DeviceEventIngestion_DeduplicatesSourceEventAndCorrelatesRepeatedAlerts()
    {
        var graph = await SeedPrerequisitesAsync();
        var eventId = Guid.NewGuid();
        var publisher = new NoOpRealtimeNotificationPublisher();
        var criticalAlertNotifier = new RecordingCriticalOperationalAlertNotifier();

        async Task<Application.Shared.Wrappers.ApiResult<Application.Devices.Telemetry.Results.DeviceEventIngestResult>> IngestAsync(
            Guid sourceEventId,
            SeverityLevel severity = SeverityLevel.Error,
            string eventType = "MotorOverheat")
        {
            await using var dbContext = _fixture.CreateDbContext();
            var telemetryStore = new EdgeTelemetryIngestionStore(dbContext);
            return await new IngestDeviceEventCommandHandler(
                telemetryStore,
                telemetryStore,
                publisher,
                criticalAlertNotifier,
                Options.Create(new EdgeTelemetryIngestionOptions()))
                .HandleAsync(new IngestDeviceEventCommand
                {
                    KioskId = graph.KioskId,
                    EndpointId = graph.EndpointId,
                    OriginNodeId = graph.SourceExecutorId,
                    DeviceId = graph.DeviceId,
                    EventId = sourceEventId,
                    EventType = eventType,
                    Severity = severity,
                    Message = "Motor temperature exceeded warning threshold.",
                    OccurredAt = DateTimeOffset.UtcNow,
                    PayloadJson = "{\"temperatureC\":85}"
                });
        }

        var first = await IngestAsync(eventId);
        var duplicate = await IngestAsync(eventId);
        var correlated = await IngestAsync(Guid.NewGuid(), SeverityLevel.Critical);
        var repeatedCritical = await IngestAsync(Guid.NewGuid(), SeverityLevel.Critical);
        var directCritical = await IngestAsync(Guid.NewGuid(), SeverityLevel.Critical, "EmergencyStop");

        Assert.True(first.Succeeded, first.Message);
        Assert.Equal(201, first.StatusCode);
        Assert.False(first.Data!.Duplicate);
        Assert.True(duplicate.Succeeded, duplicate.Message);
        Assert.True(duplicate.Data!.Duplicate);
        Assert.Equal(first.Data.DeviceEventId, duplicate.Data.DeviceEventId);
        Assert.True(correlated.Succeeded, correlated.Message);
        Assert.False(correlated.Data!.Duplicate);
        Assert.True(repeatedCritical.Succeeded, repeatedCritical.Message);
        Assert.True(directCritical.Succeeded, directCritical.Message);
        Assert.Equal(4, publisher.DeviceEventCreatedEvents.Count);
        Assert.Equal(4, publisher.AlertChangedEvents.Count);
        Assert.Collection(
            criticalAlertNotifier.Notifications,
            push =>
            {
                Assert.Equal("MotorOverheat", push.AlertCode);
                Assert.Equal(graph.KioskId, push.KioskId);
            },
            push => Assert.Equal("EmergencyStop", push.AlertCode));
        var alertNotification = Assert.Single(publisher.AlertChangedEvents,
            notification => notification.AlertCode == "MotorOverheat" && notification.OccurrenceCount == 3);
        Assert.Equal("Open", alertNotification.OldStatus);
        Assert.Equal("Open", alertNotification.NewStatus);
        Assert.Equal("MotorOverheat", alertNotification.AlertCode);
        Assert.Equal(3, alertNotification.OccurrenceCount);

        await using var assertionContext = _fixture.CreateDbContext();
        Assert.Equal(4, await assertionContext.DeviceEvents.CountAsync(item => item.KioskId == graph.KioskId));
        var alert = Assert.Single(await assertionContext.Alerts
            .Where(item => item.KioskId == graph.KioskId && item.AlertCode == "MotorOverheat")
            .ToListAsync());
        Assert.Equal(AlertStatus.Open, alert.Status);
        Assert.Equal(SeverityLevel.Critical, alert.Severity);
        Assert.Equal(3, alert.OccurrenceCount);
        Assert.Equal(repeatedCritical.Data!.DeviceEventId, alert.SourceId);
    }

    [IntegrationFact]
    public async Task BatchEventSync_ReplaysMixedTelemetryWithoutDuplicates()
    {
        var graph = await SeedPrerequisitesAsync();
        var publisher = new NoOpRealtimeNotificationPublisher();
        var heartbeatEventId = Guid.NewGuid();
        var deviceEventId = Guid.NewGuid();
        var localLogEventId = Guid.NewGuid();

        async Task<Application.Shared.Wrappers.ApiResult<Application.Devices.Telemetry.Results.BatchEventSyncResult>> IngestAsync()
        {
            await using var dbContext = _fixture.CreateDbContext();
            var telemetryStore = new EdgeTelemetryIngestionStore(dbContext);
            var options = Options.Create(new EdgeTelemetryIngestionOptions());
            return await new IngestBatchEventsCommandHandler(
                    new BatchEventSyncStore(dbContext),
                    new IngestKioskHeartbeatCommandHandler(telemetryStore, publisher, options),
                    new IngestDeviceEventCommandHandler(telemetryStore, telemetryStore, publisher,
                        new RecordingCriticalOperationalAlertNotifier(), options),
                    new IngestLocalOperationLogCommandHandler(telemetryStore, options),
                    options,
                    NullLogger<IngestBatchEventsCommandHandler>.Instance)
                .HandleAsync(new IngestBatchEventsCommand
                {
                    KioskId = graph.KioskId,
                    EndpointId = graph.EndpointId,
                    OriginNodeId = graph.SourceExecutorId,
                    Events =
                    [
                        new BatchSyncEventItem
                        {
                            EventId = heartbeatEventId,
                            EventType = BatchSyncEventType.Heartbeat,
                            Heartbeat = new BatchHeartbeatData
                            {
                                HeartbeatSequence = 991,
                                ReportedAt = DateTimeOffset.UtcNow,
                                Status = KioskHeartbeatStatus.Online
                            }
                        },
                        new BatchSyncEventItem
                        {
                            EventId = deviceEventId,
                            EventType = BatchSyncEventType.DeviceEvent,
                            DeviceEvent = new BatchDeviceEventData
                            {
                                DeviceId = graph.DeviceId,
                                EventType = "BatchMotorFault",
                                Severity = SeverityLevel.Error,
                                Message = "Motor fault replayed from local storage.",
                                OccurredAt = DateTimeOffset.UtcNow
                            }
                        },
                        new BatchSyncEventItem
                        {
                            EventId = localLogEventId,
                            EventType = BatchSyncEventType.LocalLog,
                            LocalLog = new BatchLocalLogData
                            {
                                DeviceId = graph.DeviceId,
                                Action = "RuntimeRestarted",
                                Category = "EdgeRuntime",
                                Severity = SeverityLevel.Info,
                                Message = "Runtime restarted after a local power interruption.",
                                OccurredAt = DateTimeOffset.UtcNow
                            }
                        }
                    ]
                });
        }

        var first = await IngestAsync();
        var replay = await IngestAsync();

        Assert.True(first.Succeeded, first.Message);
        Assert.Equal(3, first.Data!.AcceptedCount);
        Assert.Equal(0, first.Data.DuplicateCount);
        Assert.True(replay.Succeeded, replay.Message);
        Assert.Equal(3, replay.Data!.DuplicateCount);

        await using var assertionContext = _fixture.CreateDbContext();
        Assert.Equal(3, await assertionContext.SyncEventInbox.CountAsync(item =>
            item.EventId == heartbeatEventId || item.EventId == deviceEventId || item.EventId == localLogEventId));
        Assert.Single(await assertionContext.OperationLogs.Where(item => item.SourceEventId == localLogEventId).ToListAsync());
        var persistedDeviceEvent = Assert.Single(
            await assertionContext.DeviceEvents.Where(item => item.EventId == deviceEventId).ToListAsync());
        Assert.Single(await assertionContext.Alerts.Where(item => item.SourceId == persistedDeviceEvent.Id).ToListAsync());
    }

    [IntegrationFact]
    public async Task RetentionPurge_PreservesTicketEvidenceAndNonTerminalInbox()
    {
        var graph = await SeedPrerequisitesAsync();
        var now = DateTimeOffset.UtcNow;
        var oldTimestamp = now.AddDays(-200);
        var protectedEvent = new DeviceEvent
        {
            DeviceId = graph.DeviceId,
            KioskId = graph.KioskId,
            EventId = Guid.NewGuid(),
            EventType = "ProtectedEvidence",
            Severity = SeverityLevel.Error,
            Message = "Referenced by maintenance ticket.",
            OccurredAt = oldTimestamp,
            OriginNodeId = graph.SourceExecutorId,
            Version = 1
        };
        var deletableEvent = new DeviceEvent
        {
            DeviceId = graph.DeviceId,
            KioskId = graph.KioskId,
            EventId = Guid.NewGuid(),
            EventType = "ExpiredEvidence",
            Severity = SeverityLevel.Warning,
            Message = "Unreferenced old evidence.",
            OccurredAt = oldTimestamp,
            OriginNodeId = graph.SourceExecutorId,
            Version = 1
        };
        var processedInboxId = Guid.NewGuid();
        var failedInboxId = Guid.NewGuid();
        var expiredRefreshTokenId = Guid.NewGuid();
        var activeRefreshTokenId = Guid.NewGuid();
        var expiredPasswordResetId = Guid.NewGuid();
        var expiredInvitationId = Guid.NewGuid();
        var deliveredNotification = NotificationDelivery.CreatePush(
            graph.OrganizationId, graph.StoreId, graph.KioskId,
            Guid.NewGuid(),
            $"retention-delivered-{Guid.NewGuid():N}", "retention", graph.AccountId,
            "Old notification", "Delivered long ago.", "{}", oldTimestamp);
        deliveredNotification.MarkProcessing(oldTimestamp, TimeSpan.FromMinutes(1));
        deliveredNotification.MarkDelivered(oldTimestamp.AddMinutes(1));
        var durableEvidenceNotification = NotificationDelivery.CreatePush(
            graph.OrganizationId, graph.StoreId, graph.KioskId,
            Guid.NewGuid(),
            $"retention-durable-{Guid.NewGuid():N}", "deployment_failed", graph.AccountId,
            "Old deployment failure", "Must retain idempotency evidence.", "{}", oldTimestamp);
        durableEvidenceNotification.MarkProcessing(oldTimestamp, TimeSpan.FromMinutes(1));
        durableEvidenceNotification.MarkDelivered(oldTimestamp.AddMinutes(1));
        var pendingNotification = NotificationDelivery.CreatePush(
            graph.OrganizationId, graph.StoreId, graph.KioskId,
            Guid.NewGuid(),
            $"retention-pending-{Guid.NewGuid():N}", "retention", graph.AccountId,
            "Pending notification", "Must be retained.", "{}", oldTimestamp);

        await using var dbContext = _fixture.CreateDbContext();
        dbContext.AddRange(
            protectedEvent,
            deletableEvent,
            new KioskHeartbeat
            {
                KioskId = graph.KioskId,
                NodeId = graph.SourceExecutorId,
                OriginNodeId = graph.SourceExecutorId,
                HeartbeatSequence = 7001,
                Version = 7001,
                ReportedAt = oldTimestamp,
                ReceivedAt = oldTimestamp,
                Status = KioskHeartbeatStatus.Online
            },
            new OperationLog
            {
                KioskId = graph.KioskId,
                SourceEventId = Guid.NewGuid(),
                Action = "OldLog",
                Category = "RetentionTest",
                Severity = SeverityLevel.Info,
                Message = "Old local log.",
                OccurredAt = oldTimestamp,
                OriginNodeId = graph.SourceExecutorId,
                Version = 1
            },
            new SyncEventInbox
            {
                Id = processedInboxId,
                EventId = Guid.NewGuid(),
                KioskId = graph.KioskId,
                SourceNodeId = graph.SourceExecutorId,
                EventType = "Retention.Processed",
                PayloadJson = "{}",
                Status = SyncEventStatus.Processed,
                OccurredAt = oldTimestamp,
                ReceivedAt = oldTimestamp,
                ProcessedAt = oldTimestamp
            },
            new SyncEventInbox
            {
                Id = failedInboxId,
                EventId = Guid.NewGuid(),
                KioskId = graph.KioskId,
                SourceNodeId = graph.SourceExecutorId,
                EventType = "Retention.Failed",
                PayloadJson = "{}",
                Status = SyncEventStatus.Failed,
                OccurredAt = oldTimestamp,
                ReceivedAt = oldTimestamp,
                LastError = "Keep for retry investigation."
            },
            new RefreshToken
            {
                Id = expiredRefreshTokenId,
                AccountId = graph.AccountId,
                TokenHash = $"expired-{Guid.NewGuid():N}",
                CreatedAt = oldTimestamp.AddDays(-1),
                ExpiresAt = oldTimestamp
            },
            new RefreshToken
            {
                Id = activeRefreshTokenId,
                AccountId = graph.AccountId,
                TokenHash = $"active-{Guid.NewGuid():N}",
                CreatedAt = now,
                ExpiresAt = now.AddDays(7)
            },
            new PasswordResetRequest
            {
                Id = expiredPasswordResetId,
                AccountId = graph.AccountId,
                TokenHash = $"reset-{Guid.NewGuid():N}",
                RequestedAt = oldTimestamp.AddHours(-1),
                ExpiresAt = oldTimestamp
            },
            new AccountInvitation
            {
                Id = expiredInvitationId,
                AccountId = graph.AccountId,
                TokenHash = $"invite-{Guid.NewGuid():N}",
                InvitedAt = oldTimestamp.AddDays(-1),
                ExpiresAt = oldTimestamp
            },
            deliveredNotification,
            durableEvidenceNotification,
            pendingNotification);
        await dbContext.SaveChangesAsync();
        dbContext.MaintenanceTickets.Add(new MaintenanceTicket
        {
            OrganizationId = graph.OrganizationId,
            StoreId = graph.StoreId,
            KioskId = graph.KioskId,
            DeviceId = graph.DeviceId,
            DeviceEventId = protectedEvent.Id,
            TicketNumber = $"MT-{Guid.NewGuid():N}",
            IssueCode = "DEVICE_EVENT_EVIDENCE",
            Title = "Protected retention evidence",
            Priority = MaintenancePriority.Medium,
            Status = MaintenanceTicketStatus.Open,
            ReportedAt = oldTimestamp
        });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var purger = new DataRetentionPurger(
            dbContext,
            Options.Create(new DataRetentionOptions
            {
                HeartbeatDays = 30,
                DeviceEventDays = 90,
                OperationLogDays = 90,
                ProcessedSyncInboxDays = 180,
                ExpiredIdentityCredentialDays = 30,
                NotificationDeliveryDays = 90,
                BatchSize = 1,
                MaxBatchesPerRun = 10
            }));
        var result = await purger.PurgeAsync(now);
        dbContext.ChangeTracker.Clear();

        Assert.Equal(1, result.Heartbeats);
        Assert.Equal(1, result.DeviceEvents);
        Assert.Equal(1, result.OperationLogs);
        Assert.Equal(1, result.SyncInboxReceipts);
        Assert.Equal(1, result.RefreshTokens);
        Assert.Equal(1, result.PasswordResetRequests);
        Assert.Equal(1, result.AccountInvitations);
        Assert.Equal(1, result.NotificationDeliveries);
        Assert.True(await dbContext.DeviceEvents.AnyAsync(item => item.Id == protectedEvent.Id));
        Assert.False(await dbContext.DeviceEvents.AnyAsync(item => item.Id == deletableEvent.Id));
        Assert.False(await dbContext.SyncEventInbox.AnyAsync(item => item.Id == processedInboxId));
        Assert.True(await dbContext.SyncEventInbox.AnyAsync(item => item.Id == failedInboxId));
        Assert.False(await dbContext.RefreshTokens.AnyAsync(item => item.Id == expiredRefreshTokenId));
        Assert.True(await dbContext.RefreshTokens.AnyAsync(item => item.Id == activeRefreshTokenId));
        Assert.False(await dbContext.PasswordResetRequests.AnyAsync(item => item.Id == expiredPasswordResetId));
        Assert.False(await dbContext.AccountInvitations.AnyAsync(item => item.Id == expiredInvitationId));
        Assert.False(await dbContext.NotificationDeliveries.AnyAsync(item => item.Id == deliveredNotification.Id));
        Assert.True(await dbContext.NotificationDeliveries.AnyAsync(item => item.Id == durableEvidenceNotification.Id));
        Assert.True(await dbContext.NotificationDeliveries.AnyAsync(item => item.Id == pendingNotification.Id));
    }

    [IntegrationFact]
    public async Task UploadProgramReleaseDeployAndReportActive_CompletesOperationalFlow()
    {
        var graph = await SeedPrerequisitesAsync();
        var user = new CurrentUserContext { AccountId = graph.AccountId, IsSystemAdmin = true };
        var luaBytes = Encoding.UTF8.GetBytes("print('operational-smoke')");

        Guid artifactId;
        Guid programId;
        await using (var dbContext = _fixture.CreateDbContext())
        {
            var robotArtifactStore = new RobotArtifactStore(dbContext);
            var robotProgramStore = new RobotProgramStore(dbContext);
            var mutationCoordinator = new PostgresTechnicalResourceMutationCoordinator(dbContext);
            var packageOwnership = new ProductionPackageTechnicalOwnershipPolicy(
                new ProductionPackageTechnicalOwnershipStore(dbContext));
            var technicalContractStore = new RobotArtifactTechnicalContractStore(dbContext);
            var technicalContract = RobotArtifactTechnicalContract.CreateDraft(
                $"SMOKE-{Guid.NewGuid():N}", 1, RuntimeTargetCode, MachineModelCode, graph.OrganizationId);
            technicalContract.ReplaceDefinition(
                [new RobotArtifactEffectDefinition("MAKE_ICE_CREAM", RobotArtifactEffectKind.System, null, null,
                    RobotArtifactQuantityMode.None, null, null, null)],
                []);
            technicalContract.Publish(DateTimeOffset.UtcNow, user.AccountId, parameterizedRuntimeSupported: false);
            await technicalContractStore.AddAsync(technicalContract, CancellationToken.None);
            var objectStorage = _fixture.CreateObjectStorage(autoCreateBucket: true);
            var upload = new UploadRobotArtifactCommandHandler(
                robotArtifactStore,
                new ArtifactUploadContentService(
                    objectStorage,
                    NullLogger<ArtifactUploadContentService>.Instance),
                mutationCoordinator,
                technicalContractStore);
            var bulkUpload = new BulkUploadRobotArtifactsCommandHandler(upload);
            await using var lua = new MemoryStream(luaBytes);
            var uploaded = await bulkUpload.HandleAsync(new BulkUploadRobotArtifactsCommand
            {
                UserContext = user,
                OrganizationId = graph.OrganizationId,
                Items =
                [
                    new BulkUploadRobotArtifactItem
                    {
                        FileName = "01_make_ice_cream.lua",
                        ContentType = "text/x-lua",
                        ContentLengthBytes = luaBytes.Length,
                        Content = lua,
                        ArtifactCode = $"SMOKE-{Guid.NewGuid():N}",
                        ArtifactName = "Operational smoke artifact",
                        RuntimeTargetCode = RuntimeTargetCode,
                        MachineModelCode = MachineModelCode,
                        TechnicalContractId = technicalContract.Id
                    }
                ]
            });
            Assert.True(uploaded.Succeeded, uploaded.Message);
            artifactId = Assert.Single(uploaded.Data!.Items).RobotArtifactId!.Value;

            var publishedArtifact = await new PublishRobotArtifactCommandHandler(
                robotArtifactStore,
                new ArtifactPublicationValidator(technicalContractStore, objectStorage),
                mutationCoordinator).HandleAsync(
                new PublishRobotArtifactCommand
                {
                    UserContext = user,
                    OrganizationId = graph.OrganizationId,
                    ArtifactId = artifactId
                });
            Assert.True(publishedArtifact.Succeeded, publishedArtifact.Message);

            var createdProgram = await new CreateRobotProgramCommandHandler(
                robotProgramStore, mutationCoordinator).HandleAsync(
                new CreateRobotProgramCommand
                {
                    UserContext = user,
                    OrganizationId = graph.OrganizationId,
                    Code = $"SMOKE-{Guid.NewGuid():N}",
                    Name = "Operational smoke program"
                });
            Assert.True(createdProgram.Succeeded, createdProgram.Message);
            programId = createdProgram.Data!.Id;

            var assigned = await new ReplaceRobotProgramArtifactsCommandHandler(
                robotProgramStore, robotArtifactStore, packageOwnership, mutationCoordinator).HandleAsync(
                new ReplaceRobotProgramArtifactsCommand
                {
                    UserContext = user,
                    OrganizationId = graph.OrganizationId,
                    ProgramId = programId,
                    Artifacts = [new RobotProgramArtifactInput(artifactId, 1, 1, null)]
                });
            Assert.True(assigned.Succeeded, assigned.Message);

            var publishedProgram = await new PublishRobotProgramCommandHandler(
                robotProgramStore, robotArtifactStore, mutationCoordinator).HandleAsync(
                new PublishRobotProgramCommand
                {
                    UserContext = user,
                    OrganizationId = graph.OrganizationId,
                    ProgramId = programId
                });
            Assert.True(publishedProgram.Succeeded, publishedProgram.Message);
        }

        Guid releaseId;
        Guid deploymentId;
        Guid commandId;
        await using (var dbContext = _fixture.CreateDbContext())
        {
            var releaseStore = new ConfigurationReleaseStore(dbContext);
            var routeStore = new ConfigurationRouteStore(dbContext);
            var deploymentStore = new ConfigurationDeploymentStore(dbContext);
            var packageOwnership = new ProductionPackageTechnicalOwnershipPolicy(
                new ProductionPackageTechnicalOwnershipStore(dbContext));
            var createdRelease = await new CreateConfigurationReleaseCommandHandler(releaseStore).HandleAsync(
                new CreateConfigurationReleaseCommand
                {
                    UserContext = user,
                    OrganizationId = graph.OrganizationId
                });
            Assert.True(createdRelease.Succeeded, createdRelease.Message);
            releaseId = createdRelease.Data!.Id;

            var routed = await new ReplaceConfigurationReleaseRoutesCommandHandler(
                releaseStore, routeStore, packageOwnership).HandleAsync(
                new ReplaceConfigurationReleaseRoutesCommand
                {
                    UserContext = user,
                    OrganizationId = graph.OrganizationId,
                    ReleaseId = releaseId,
                    Routes =
                    [
                        new ConfigurationReleaseRouteInput(
                            graph.RecipeId,
                            "DEFAULT",
                            0,
                            null,
                            Array.Empty<string>(),
                            [new ConfigurationReleaseRobotBindingInput(programId, 1, "ICE_CREAM")])
                    ]
                });
            Assert.True(routed.Succeeded, routed.Message);

            var inventoryReadiness = new ProductionInventoryReadinessGuard(
                new InventoryReadinessEvaluator(new InventoryStore(dbContext)),
                Options.Create(new InventoryReadinessPolicyOptions
                {
                    PublishPolicy = InventoryReadinessPolicy.Warn,
                    DeployPolicy = InventoryReadinessPolicy.Warn
                }));

            var publishedRelease = await new PublishConfigurationReleaseCommandHandler(
                releaseStore,
                inventoryReadiness,
                new ProductionDefinitionPublicationService()).HandleAsync(
                new PublishConfigurationReleaseCommand
                {
                    UserContext = user,
                    OrganizationId = graph.OrganizationId,
                    ReleaseId = releaseId
                });
            Assert.True(publishedRelease.Succeeded, publishedRelease.Message);

            var edgeStore = new EdgeCommandStore(dbContext);
            var deploymentWakeUpPublisher = new NoOpEdgeCommandWakeUpPublisher { PublishResult = false };
            var deployed = await new DeployFullEdgeConfigurationCommandHandler(
                deploymentStore,
                releaseStore,
                edgeStore,
                deploymentWakeUpPublisher,
                inventoryReadiness,
                new FullEdgeReleaseBundleService(_fixture.CreateObjectStorage(autoCreateBucket: true))).HandleAsync(
                new DeployFullEdgeConfigurationCommand
                {
                    UserContext = user,
                    KioskId = graph.KioskId,
                    ConfigurationReleaseId = releaseId,
                    KioskExecutionEndpointId = graph.EndpointId,
                    IdempotencyKey = Guid.NewGuid().ToString("N")
                });
            Assert.True(deployed.Succeeded, deployed.Message);
            deploymentId = deployed.Data!.Id;
            commandId = deployed.Data.EdgeCommandId!.Value;
            var deploymentWakeUp = Assert.Single(deploymentWakeUpPublisher.Notifications);
            Assert.Equal(commandId, deploymentWakeUp.CommandId);
            Assert.Equal(EdgeCommandType.DeployConfiguration, deploymentWakeUp.CommandType);
        }

        await PullAndAcceptAsync(graph, commandId);
        await AssertDeploymentProvenanceRejectedAsync(graph, commandId, deploymentId);
        await ReportAsync(graph, commandId, deploymentId, Guid.NewGuid(), 1, "Installed");
        await ReportAsync(graph, commandId, deploymentId, Guid.NewGuid(), 2, "Active");

        await using var assertionContext = _fixture.CreateDbContext();
        var deployment = await assertionContext.KioskConfigurationDeployments.SingleAsync(x => x.Id == deploymentId);
        var endpoint = await assertionContext.KioskExecutionEndpoints.SingleAsync(x => x.Id == graph.EndpointId);
        Assert.Equal(KioskConfigurationDeploymentStatus.Active, deployment.Status);
        Assert.Equal(deploymentId, endpoint.ActiveConfigurationDeploymentId);
        Assert.Equal(releaseId, endpoint.ActiveConfigurationReleaseId);

        await AssertInventoryDispatchBlockedAsync(graph, "inactive");
        await AssertInventoryDispatchBlockedAsync(graph, "calibration");
        await AssertInventoryDispatchBlockedAsync(graph, "device");

        var orderId = await CreatePaidOrderAsync(graph, quantity: 2);
        await using var dispatchContext = _fixture.CreateDbContext();
        var dispatchWakeUpPublisher = new NoOpEdgeCommandWakeUpPublisher { PublishResult = false };
        var dispatchHandler = new DispatchOrderExecutionCommandHandler(
            new OrderExecutionDispatchStore(dispatchContext),
            Options.Create(new OrderExecutionDispatchOptions()),
            dispatchWakeUpPublisher);
        var firstDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = orderId,
            DispatchAttemptNo = 1
        });
        Assert.True(firstDispatch.Succeeded, firstDispatch.Message);
        Assert.False(firstDispatch.Data!.Existing);
        var dispatchWakeUp = Assert.Single(dispatchWakeUpPublisher.Notifications);
        Assert.Equal(firstDispatch.Data.EdgeCommandId, dispatchWakeUp.CommandId);
        Assert.Equal(EdgeCommandType.ExecuteOrder, dispatchWakeUp.CommandType);

        var retryDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = orderId,
            DispatchAttemptNo = 1
        });
        Assert.True(retryDispatch.Succeeded, retryDispatch.Message);
        Assert.True(retryDispatch.Data!.Existing);
        Assert.Equal(firstDispatch.Data.EdgeCommandId, retryDispatch.Data.EdgeCommandId);

        var command = await dispatchContext.EdgeCommands.SingleAsync(x => x.OrderId == orderId);
        Assert.Equal(EdgeCommandType.ExecuteOrder, command.CommandType);
        Assert.Equal(graph.EndpointId, command.TargetExecutionEndpointId);
        Assert.Equal(1, command.DispatchAttemptNo);
        Assert.False(dispatchWakeUpPublisher.PublishResult);

        await PullAndAcknowledgeAsync(graph, command.Id, "Accepted");
        await using (var acceptedContext = _fixture.CreateDbContext())
        {
            var acceptedOrder = await acceptedContext.Orders.SingleAsync(x => x.Id == orderId);
            Assert.Equal(OrderStatus.Accepted, acceptedOrder.Status);
            Assert.Single(await acceptedContext.OrderStatusHistories
                .Where(x => x.OrderId == orderId && x.ToStatus == OrderStatus.Accepted)
                .ToListAsync());
        }

        var productionJobId = Guid.NewGuid();
        var stockEvidenceEventId = Guid.NewGuid();
        await ReportProductionAsync(
            graph,
            command.Id,
            productionJobId,
            1,
            "Completed",
            releaseId,
            deployment.ReleaseChecksum,
            [new StockMovementEvidenceInput(stockEvidenceEventId, graph.DispenserStateId, 10, null, null, false)]);
        await using (var jobLevelAssertionContext = _fixture.CreateDbContext())
        {
            Assert.Equal(
                OrderStatus.Accepted,
                (await jobLevelAssertionContext.Orders.SingleAsync(x => x.Id == orderId)).Status);
        }
        await ReportProductionAsync(
            graph,
            command.Id,
            null,
            1,
            "Running",
            releaseId,
            deployment.ReleaseChecksum);
        await ReportProductionAsync(
            graph,
            command.Id,
            null,
            2,
            "Completed",
            releaseId,
            deployment.ReleaseChecksum);

        await using (var completedContext = _fixture.CreateDbContext())
        {
            Assert.Equal(OrderStatus.Completed, (await completedContext.Orders.SingleAsync(x => x.Id == orderId)).Status);
            var movement = await completedContext.StockMovements.SingleAsync(x => x.SourceEventId == stockEvidenceEventId);
            Assert.Equal(-10, movement.Quantity);
            Assert.Equal(orderId, movement.ReferenceId);
            Assert.Equal(90, (await completedContext.IngredientDispenserStates
                .SingleAsync(x => x.Id == graph.DispenserStateId)).EstimatedQuantity);
        }

        var rejectedOrderId = await CreatePaidOrderAsync(graph);
        var rejectedDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = rejectedOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(rejectedDispatch.Succeeded, rejectedDispatch.Message);
        await PullAndAcknowledgeAsync(graph, rejectedDispatch.Data!.EdgeCommandId, "Rejected", false);

        var supportOrderId = await CreatePaidOrderAsync(graph);
        var supportDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = supportOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(supportDispatch.Succeeded, supportDispatch.Message);
        await PullAndAcknowledgeAsync(graph, supportDispatch.Data!.EdgeCommandId, "Rejected", true);

        var busyOrderId = await CreatePaidOrderAsync(graph);
        var busyDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = busyOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(busyDispatch.Succeeded, busyDispatch.Message);
        await PullAndAcknowledgeAsync(graph, busyDispatch.Data!.EdgeCommandId, "ExecutorBusy");
        await AcknowledgeAsync(graph, busyDispatch.Data.EdgeCommandId, "ExecutorBusy");

        await using (var busyAssertionContext = _fixture.CreateDbContext())
        {
            Assert.Equal(
                OrderStatus.ReadyForFulfillment,
                (await busyAssertionContext.Orders.SingleAsync(x => x.Id == busyOrderId)).Status);
            var busyCommandBeforeRedelivery = await busyAssertionContext.EdgeCommands
                .Include(x => x.DeliveryAttempts)
                .SingleAsync(x => x.Id == busyDispatch.Data.EdgeCommandId);
            Assert.Equal(EdgeCommandStatus.PendingDelivery, busyCommandBeforeRedelivery.Status);
            Assert.Equal(2, busyCommandBeforeRedelivery.DeliveryAttempts.Count);
        }

        var failedOrderId = await CreatePaidOrderAsync(graph);
        var failedDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = failedOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(failedDispatch.Succeeded, failedDispatch.Message);
        await PullAndAcknowledgeAsync(graph, failedDispatch.Data!.EdgeCommandId, "Accepted");
        await ReportProductionAsync(
            graph,
            failedDispatch.Data.EdgeCommandId,
            null,
            1,
            "Failed",
            releaseId,
            deployment.ReleaseChecksum);

        var interventionOrderId = await CreatePaidOrderAsync(graph);
        var interventionDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = interventionOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(interventionDispatch.Succeeded, interventionDispatch.Message);
        await PullAndAcknowledgeAsync(graph, interventionDispatch.Data!.EdgeCommandId, "Accepted");
        await ReportProductionAsync(
            graph,
            interventionDispatch.Data.EdgeCommandId,
            null,
            1,
            "RequiresManualIntervention",
            releaseId,
            deployment.ReleaseChecksum);

        var concurrentEvidenceOrderId = await CreatePaidOrderAsync(graph, quantity: 2);
        var concurrentEvidenceDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = concurrentEvidenceOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(concurrentEvidenceDispatch.Succeeded, concurrentEvidenceDispatch.Message);
        await PullAndAcknowledgeAsync(graph, concurrentEvidenceDispatch.Data!.EdgeCommandId, "Accepted");
        var sharedStockEvidenceId = Guid.NewGuid();
        var sharedEvidence = new StockMovementEvidenceInput(
            sharedStockEvidenceId,
            graph.DispenserStateId,
            5,
            85,
            null,
            false);
        await Task.WhenAll(
            ReportProductionAsync(
                graph,
                concurrentEvidenceDispatch.Data.EdgeCommandId,
                Guid.NewGuid(),
                1,
                "Completed",
                releaseId,
                deployment.ReleaseChecksum,
                [sharedEvidence],
                productionUnitNo: 1),
            ReportProductionAsync(
                graph,
                concurrentEvidenceDispatch.Data.EdgeCommandId,
                Guid.NewGuid(),
                1,
                "Completed",
                releaseId,
                deployment.ReleaseChecksum,
                [sharedEvidence],
                productionUnitNo: 2));

        var reusedEvidenceOrderId = await CreatePaidOrderAsync(graph);
        var reusedEvidenceDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = reusedEvidenceOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(reusedEvidenceDispatch.Succeeded, reusedEvidenceDispatch.Message);
        await PullAndAcknowledgeAsync(graph, reusedEvidenceDispatch.Data!.EdgeCommandId, "Accepted");
        var reusedEvidence = await IngestProductionAsync(
            graph,
            reusedEvidenceDispatch.Data.EdgeCommandId,
            Guid.NewGuid(),
            1,
            "Completed",
            releaseId,
            deployment.ReleaseChecksum,
            [sharedEvidence with { QuantityConsumed = 1, BalanceAfter = 84 }]);
        Assert.False(reusedEvidence.Succeeded);
        Assert.Equal(400, reusedEvidence.StatusCode);
        Assert.Equal(
            "Stock movement source event id was reused with different evidence.",
            reusedEvidence.Message);

        var inconsistentBalanceOrderId = await CreatePaidOrderAsync(graph);
        var inconsistentBalanceDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = inconsistentBalanceOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(inconsistentBalanceDispatch.Succeeded, inconsistentBalanceDispatch.Message);
        await PullAndAcknowledgeAsync(graph, inconsistentBalanceDispatch.Data!.EdgeCommandId, "Accepted");
        var inconsistentBalanceEvidenceId = Guid.NewGuid();
        var inconsistentBalance = await IngestProductionAsync(
            graph,
            inconsistentBalanceDispatch.Data.EdgeCommandId,
            Guid.NewGuid(),
            1,
            "Completed",
            releaseId,
            deployment.ReleaseChecksum,
            [new StockMovementEvidenceInput(
                inconsistentBalanceEvidenceId,
                graph.DispenserStateId,
                5,
                81,
                null,
                false)]);
        Assert.False(inconsistentBalance.Succeeded);
        Assert.Equal(400, inconsistentBalance.StatusCode);
        Assert.Equal(
            "Reported stock balance does not match the dispenser estimate after consumption.",
            inconsistentBalance.Message);

        await using (var inconsistentBalanceContext = _fixture.CreateDbContext())
        {
            Assert.Equal(85, (await inconsistentBalanceContext.IngredientDispenserStates
                .SingleAsync(state => state.Id == graph.DispenserStateId)).EstimatedQuantity);
            Assert.False(await inconsistentBalanceContext.StockMovements
                .AnyAsync(movement => movement.SourceEventId == inconsistentBalanceEvidenceId));
            Assert.False(await inconsistentBalanceContext.ProductionExecutionRecords
                .AnyAsync(record => record.SourceCommandId == inconsistentBalanceDispatch.Data.EdgeCommandId));
            Assert.Equal(
                OrderStatus.Accepted,
                (await inconsistentBalanceContext.Orders
                    .SingleAsync(order => order.Id == inconsistentBalanceOrderId)).Status);
            Assert.Equal(
                OrderStatus.Accepted,
                (await inconsistentBalanceContext.Orders
                    .SingleAsync(order => order.Id == reusedEvidenceOrderId)).Status);
            Assert.False(await inconsistentBalanceContext.ProductionExecutionRecords
                .AnyAsync(record => record.SourceCommandId == reusedEvidenceDispatch.Data.EdgeCommandId));
        }

        var refillReasonOne = $"CONCURRENT_REFILL_{Guid.NewGuid():N}";
        var refillReasonTwo = $"CONCURRENT_REFILL_{Guid.NewGuid():N}";
        await Task.WhenAll(
            RefillAsync(graph, user, 5, refillReasonOne),
            RefillAsync(graph, user, 5, refillReasonTwo));
        await using (var refillAssertionContext = _fixture.CreateDbContext())
        {
            Assert.Equal(95, (await refillAssertionContext.IngredientDispenserStates
                .SingleAsync(state => state.Id == graph.DispenserStateId)).EstimatedQuantity);
            Assert.Equal(2, await refillAssertionContext.StockMovements.CountAsync(movement =>
                movement.ReasonCode == refillReasonOne || movement.ReasonCode == refillReasonTwo));
        }

        var releaseMismatchOrderId = await CreatePaidOrderAsync(graph);
        var releaseMismatchDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = releaseMismatchOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(releaseMismatchDispatch.Succeeded, releaseMismatchDispatch.Message);
        await PullAndAcknowledgeAsync(graph, releaseMismatchDispatch.Data!.EdgeCommandId, "Accepted");
        await using (var mismatchContext = _fixture.CreateDbContext())
        {
            var mismatchStore = new ExecutionReportStore(mismatchContext);
            var mismatch = await new IngestExecutionReportCommandHandler(
                mismatchStore,
                new NoOpRealtimeNotificationPublisher(),
                Options.Create(new ExecutionReportIngestionOptions()))
                .HandleAsync(new IngestExecutionReportCommand
                {
                    KioskId = graph.KioskId,
                    EndpointId = graph.EndpointId,
                    CommandId = releaseMismatchDispatch.Data.EdgeCommandId,
                    SourceEventId = Guid.NewGuid(),
                    SequenceNumber = 1,
                    EdgeCreatedAt = DateTimeOffset.UtcNow,
                    ReportType = "ProductionExecution",
                    Status = "Running",
                    SourceConfigurationReleaseId = releaseId,
                    ReleaseChecksum = new string('f', 64),
                    PhysicalOutputMayHaveOccurred = false
                });
            Assert.False(mismatch.Succeeded);
            Assert.Equal(400, mismatch.StatusCode);
            Assert.Equal("Production execution report release does not match the dispatched command.", mismatch.Message);
        }

        await using var ackAssertionContext = _fixture.CreateDbContext();
        Assert.Equal(
            OrderStatus.ExecutionRejected,
            (await ackAssertionContext.Orders.SingleAsync(x => x.Id == rejectedOrderId)).Status);
        Assert.Equal(
            OrderStatus.RefundRequired,
            (await ackAssertionContext.Orders.SingleAsync(x => x.Id == supportOrderId)).Status);
        Assert.Equal(
            OrderStatus.FulfillmentIssue,
            (await ackAssertionContext.Orders.SingleAsync(x => x.Id == failedOrderId)).Status);
        Assert.Equal(
            OrderStatus.FulfillmentIssue,
            (await ackAssertionContext.Orders.SingleAsync(x => x.Id == interventionOrderId)).Status);
        Assert.Single(await ackAssertionContext.StockMovements
            .Where(x => x.SourceEventId == sharedStockEvidenceId)
            .ToListAsync());
        Assert.Empty(await ackAssertionContext.ProductionExecutionRecords
            .Where(x => x.SourceCommandId == releaseMismatchDispatch.Data.EdgeCommandId)
            .ToListAsync());

        var attempts = await new GetOrderExecutionAttemptsQueryHandler(new OrderStore(ackAssertionContext))
            .HandleAsync(new GetOrderExecutionAttemptsQuery
            {
                OrderId = orderId,
                UserContext = user
            });
        Assert.True(attempts.Succeeded, attempts.Message);
        var attempt = Assert.Single(attempts.Data!);
        Assert.Equal(command.Id, attempt.SourceCommandId);
        Assert.Equal("Completed", attempt.ExecutionStatus);

        var attemptDetail = await new GetExecutionAttemptQueryHandler(new OrderStore(ackAssertionContext))
            .HandleAsync(new GetExecutionAttemptQuery
            {
                SourceCommandId = command.Id,
                UserContext = user
            });
        Assert.True(attemptDetail.Succeeded, attemptDetail.Message);
        var productionExecution = Assert.Single(attemptDetail.Data!.ProductionExecutions);
        Assert.NotEqual(Guid.Empty, productionExecution.SourceProductionJobId);
        Assert.NotEmpty(attemptDetail.Data.DeliveryAttempts);
        Assert.False(attemptDetail.Data.Provenance.IsRedispatch);
        Assert.Null(attemptDetail.Data.PreviousAttempt);

        var expiryOrderId = await CreatePaidOrderAsync(graph);
        var expiryBase = DateTimeOffset.UtcNow;
        var expiryDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = expiryOrderId,
            DispatchAttemptNo = 1,
            CommandExpiryAt = expiryBase.AddMinutes(1)
        });
        Assert.True(expiryDispatch.Succeeded, expiryDispatch.Message);
        await ReconcileTimeoutAsync(graph, expiryDispatch.Data!.EdgeCommandId, expiryBase.AddMinutes(2));

        var unreachableOrderId = await CreatePaidOrderAsync(graph);
        var unreachableDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = unreachableOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(unreachableDispatch.Succeeded, unreachableDispatch.Message);
        await PullAndAcknowledgeAsync(graph, unreachableDispatch.Data!.EdgeCommandId, "Accepted");
        var unreachableObservedAt = DateTimeOffset.UtcNow.AddMinutes(6);
        var unreachablePublisher = await ReconcileTimeoutAsync(
            graph,
            unreachableDispatch.Data.EdgeCommandId,
            unreachableObservedAt);
        Assert.Single(unreachablePublisher.OrderExecutionObservationEvents);
        Assert.Equal("PendingRecovery", unreachablePublisher.OrderExecutionObservationEvents[0].CustomerStatus);

        var staleOrderId = await CreatePaidOrderAsync(graph);
        var staleDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = staleOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(staleDispatch.Succeeded, staleDispatch.Message);
        await PullAndAcknowledgeAsync(graph, staleDispatch.Data!.EdgeCommandId, "Accepted");
        await ReportProductionAsync(
            graph,
            staleDispatch.Data.EdgeCommandId,
            null,
            1,
            "Running",
            releaseId,
            deployment.ReleaseChecksum);
        var staleObservedAt = DateTimeOffset.UtcNow.AddMinutes(31);
        await using (var heartbeatContext = _fixture.CreateDbContext())
        {
            heartbeatContext.KioskHeartbeats.Add(new KioskHeartbeat
            {
                KioskId = graph.KioskId,
                NodeId = graph.SourceExecutorId,
                OriginNodeId = graph.SourceExecutorId,
                Version = 1,
                ReportedAt = staleObservedAt,
                ReceivedAt = staleObservedAt,
                Status = KioskHeartbeatStatus.Online
            });
            await heartbeatContext.SaveChangesAsync();
        }
        await ReconcileTimeoutAsync(graph, staleDispatch.Data.EdgeCommandId, staleObservedAt);

        await using var timeoutAssertionContext = _fixture.CreateDbContext();
        Assert.Equal(OrderStatus.ExecutionRejected,
            (await timeoutAssertionContext.Orders.SingleAsync(x => x.Id == expiryOrderId)).Status);
        Assert.Equal(OrderStatus.Accepted,
            (await timeoutAssertionContext.Orders.SingleAsync(x => x.Id == unreachableOrderId)).Status);
        Assert.Equal(OrderStatus.Preparing,
            (await timeoutAssertionContext.Orders.SingleAsync(x => x.Id == staleOrderId)).Status);
        var unreachableRecord = await timeoutAssertionContext.OrderExecutionRecords
            .SingleAsync(x => x.SourceCommandId == unreachableDispatch.Data.EdgeCommandId);
        Assert.Equal(ExecutionObservationStatus.Unreachable, unreachableRecord.ObservationStatus);
        Assert.Equal(CustomerExecutionStatus.PendingRecovery, unreachableRecord.CustomerExecutionStatus);
        var staleRecord = await timeoutAssertionContext.OrderExecutionRecords
            .SingleAsync(x => x.SourceCommandId == staleDispatch.Data.EdgeCommandId);
        Assert.Equal(ExecutionObservationStatus.Stale, staleRecord.ObservationStatus);
        Assert.Equal(CustomerExecutionStatus.Delayed, staleRecord.CustomerExecutionStatus);

        var supportPublisher = await ReconcileTimeoutAsync(
            graph,
            unreachableDispatch.Data.EdgeCommandId,
            staleObservedAt.AddMinutes(10));
        var supportEvent = Assert.Single(supportPublisher.OrderExecutionObservationEvents);
        Assert.Equal("SupportRequired", supportEvent.CustomerStatus);
        Assert.True(supportEvent.RequiresStaffSupport);

        await using (var supportAssertionContext = _fixture.CreateDbContext())
        {
            var supportRecord = await supportAssertionContext.OrderExecutionRecords
                .SingleAsync(x => x.SourceCommandId == unreachableDispatch.Data.EdgeCommandId);
            Assert.Equal(CustomerExecutionStatus.SupportRequired, supportRecord.CustomerExecutionStatus);
            Assert.Equal(OrderStatus.Accepted,
                (await supportAssertionContext.Orders.SingleAsync(x => x.Id == unreachableOrderId)).Status);

            var customerResult = await new GetOrderStatusQueryHandler(new OrderStore(supportAssertionContext))
                .HandleAsync(new GetOrderStatusQuery { OrderId = unreachableOrderId });
            Assert.True(customerResult.Succeeded, customerResult.Message);
            Assert.Equal("SupportRequired", customerResult.Data!.CustomerStatus);
            Assert.True(customerResult.Data.RequiresStaffSupport);
        }

        var redispatch = await RedispatchAsync(expiryOrderId, user, "Operator confirmed safe retry after expiry.");
        Assert.True(redispatch.Succeeded, redispatch.Message);
        Assert.Equal(2, redispatch.Data!.DispatchAttemptNo);
        Assert.False(redispatch.Data.Existing);
        var repeatedRedispatch = await RedispatchAsync(expiryOrderId, user, "Repeated client request.");
        Assert.True(repeatedRedispatch.Succeeded, repeatedRedispatch.Message);
        Assert.True(repeatedRedispatch.Data!.Existing);
        Assert.Equal(redispatch.Data.EdgeCommandId, repeatedRedispatch.Data.EdgeCommandId);

        await using (var provenanceContext = _fixture.CreateDbContext())
        {
            var provenanceStore = new OrderStore(provenanceContext);
            var expiredAttemptDetail = await new GetExecutionAttemptQueryHandler(provenanceStore)
                .HandleAsync(new GetExecutionAttemptQuery
                {
                    SourceCommandId = expiryDispatch.Data.EdgeCommandId,
                    UserContext = user
                });
            Assert.True(expiredAttemptDetail.Succeeded, expiredAttemptDetail.Message);
            Assert.True(expiredAttemptDetail.Data!.Provenance.TimedOutBeforeAcceptance);
            Assert.Equal(redispatch.Data.EdgeCommandId, expiredAttemptDetail.Data.NextAttempt!.SourceCommandId);

            var redispatchDetail = await new GetExecutionAttemptQueryHandler(provenanceStore)
                .HandleAsync(new GetExecutionAttemptQuery
                {
                    SourceCommandId = redispatch.Data.EdgeCommandId,
                    UserContext = user
                });
            Assert.True(redispatchDetail.Succeeded, redispatchDetail.Message);
            Assert.True(redispatchDetail.Data!.Provenance.IsRedispatch);
            Assert.Equal(expiryDispatch.Data.EdgeCommandId, redispatchDetail.Data.Provenance.RetryOfSourceCommandId);
            Assert.Equal(expiryDispatch.Data.EdgeCommandId, redispatchDetail.Data.PreviousAttempt!.SourceCommandId);
            Assert.Contains("Operator confirmed safe retry after expiry.", redispatchDetail.Data.Provenance.RedispatchReason);
        }

        var unsafeRedispatch = await RedispatchAsync(supportOrderId, user, "Unsafe retry must be rejected.");
        Assert.False(unsafeRedispatch.Succeeded);
        Assert.Equal(409, unsafeRedispatch.StatusCode);

        var deliveryFailureOrderId = await CreatePaidOrderAsync(graph);
        var deliveryFailureDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = deliveryFailureOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(deliveryFailureDispatch.Succeeded, deliveryFailureDispatch.Message);
        await PullAndAcknowledgeAsync(graph, deliveryFailureDispatch.Data!.EdgeCommandId, "DeliveryFailed");
        var deliveryRedispatch = await RedispatchAsync(
            deliveryFailureOrderId,
            user,
            "Transport delivery failed; retry approved.");
        Assert.True(deliveryRedispatch.Succeeded, deliveryRedispatch.Message);
        Assert.Equal(2, deliveryRedispatch.Data!.DispatchAttemptNo);

        var maxAttemptOrderId = await CreatePaidOrderAsync(graph);
        var maxAttemptDispatch = await dispatchHandler.HandleAsync(new DispatchOrderExecutionCommand
        {
            OrderId = maxAttemptOrderId,
            DispatchAttemptNo = 1
        });
        Assert.True(maxAttemptDispatch.Succeeded, maxAttemptDispatch.Message);
        await PullAndAcknowledgeAsync(graph, maxAttemptDispatch.Data!.EdgeCommandId, "DeliveryFailed");
        var maxAttemptResult = await RedispatchAsync(
            maxAttemptOrderId,
            user,
            "Attempt limit test.",
            maxDispatchAttempts: 1);
        Assert.False(maxAttemptResult.Succeeded);
        Assert.Equal(409, maxAttemptResult.StatusCode);

        await using var redispatchAssertionContext = _fixture.CreateDbContext();
        var redispatchedCommand = await redispatchAssertionContext.EdgeCommands
            .SingleAsync(x => x.Id == redispatch.Data.EdgeCommandId);
        Assert.Equal(user.AccountId, redispatchedCommand.CreatedByAccountId);
        Assert.Contains(await redispatchAssertionContext.OrderStatusHistories
            .Where(x => x.OrderId == expiryOrderId && x.ChangedByAccountId == user.AccountId)
            .Select(x => x.Reason!)
            .ToListAsync(), reason => reason.Contains("Operator confirmed safe retry after expiry."));
    }

    private async Task<Application.Shared.Wrappers.ApiResult<Application.EdgeIntegration.Dispatch.Results.OrderExecutionDispatchResult>> RedispatchAsync(
        Guid orderId,
        CurrentUserContext user,
        string reason,
        int maxDispatchAttempts = 3)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var options = Options.Create(new OrderExecutionDispatchOptions
        {
            MaxDispatchAttempts = maxDispatchAttempts
        });
        var handler = new RedispatchOrderExecutionCommandHandler(
            new OrderStore(dbContext),
            new DispatchOrderExecutionCommandHandler(
                new OrderExecutionDispatchStore(dbContext),
                options,
                new NoOpEdgeCommandWakeUpPublisher()),
            new NoOpRealtimeNotificationPublisher());
        return await handler.HandleAsync(new RedispatchOrderExecutionCommand
        {
            OrderId = orderId,
            UserContext = user,
            Reason = reason
        });
    }

    private async Task<NoOpRealtimeNotificationPublisher> ReconcileTimeoutAsync(
        SmokeGraph graph,
        Guid commandId,
        DateTimeOffset observedAt)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var store = new OrderExecutionTimeoutStore(dbContext);
        var candidates = await store.ListCandidateCommandIdsAsync(
            observedAt,
            observedAt.AddMinutes(-5),
            observedAt.AddMinutes(-30),
            100);
        Assert.Contains(commandId, candidates);
        var publisher = new NoOpRealtimeNotificationPublisher();
        var handler = new ReconcileOrderExecutionTimeoutCommandHandler(
            store,
            publisher,
            Options.Create(new OrderExecutionDispatchOptions()));
        await handler.HandleAsync(new ReconcileOrderExecutionTimeoutCommand
        {
            SourceCommandId = commandId,
            ObservedAt = observedAt
        });
        return publisher;
    }

    private async Task PullAndAcknowledgeAsync(
        SmokeGraph graph,
        Guid commandId,
        string status,
        bool? physicalOutputMayHaveOccurred = null)
    {
        await using (var dbContext = _fixture.CreateDbContext())
        {
            var pulled = await new PullEdgeCommandsCommandHandler(
                new EdgeCommandStore(dbContext),
                new ArtifactCommandPayloadEnricher(_fixture.CreateObjectStorage()))
                .HandleAsync(new PullEdgeCommandsCommand
                {
                    KioskId = graph.KioskId,
                    EndpointId = graph.EndpointId,
                    MaxCommands = 10
                });
            Assert.True(pulled.Succeeded, pulled.Message);
            Assert.Contains(pulled.Data!.Commands, item => item.CommandId == commandId);
        }

        await AcknowledgeAsync(graph, commandId, status, physicalOutputMayHaveOccurred);
    }

    private async Task AcknowledgeAsync(
        SmokeGraph graph,
        Guid commandId,
        string status,
        bool? physicalOutputMayHaveOccurred = null)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var acknowledged = await new AcknowledgeEdgeCommandCommandHandler(
            new EdgeCommandStore(dbContext),
            new NoOpRealtimeNotificationPublisher())
            .HandleAsync(new AcknowledgeEdgeCommandCommand
            {
                KioskId = graph.KioskId,
                EndpointId = graph.EndpointId,
                CommandId = commandId,
                AckStatus = status,
                AcknowledgedAt = DateTimeOffset.UtcNow,
                RejectionCode = status == "Rejected" ? "ReadinessRejected" : null,
                PhysicalOutputMayHaveOccurred = physicalOutputMayHaveOccurred
            });
        Assert.True(acknowledged.Succeeded, acknowledged.Message);
    }

    private async Task<Guid> CreatePaidOrderAsync(SmokeGraph graph, int quantity = 1)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var order = new Order
        {
            OrganizationId = graph.OrganizationId,
            StoreId = graph.StoreId,
            KioskId = graph.KioskId,
            OrderNumber = $"SMOKE-{Guid.NewGuid():N}"
        };
        order.SetCurrency("VND");
        order.AddItem(
            graph.MenuItemId,
            graph.ProductId,
            graph.ProductVariantId,
            graph.RecipeId,
            "SMOKE-MENU-ITEM",
            "Operational smoke item",
            "SMOKE-PRODUCT",
            "Operational smoke product",
            "SMOKE-VARIANT",
            "Operational smoke variant",
            1,
            Domain.Catalog.Enums.FulfillmentType.MachineProduced,
            quantity,
            1,
            recipeSnapshotJson: JsonSerializer.Serialize(new
            {
                Ingredients = new[] { new { graph.IngredientId } }
            }));
        var now = DateTimeOffset.UtcNow;
        order.Place(now);
        order.MarkPaid(order.TotalAmount, now);
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        return order.Id;
    }

    private async Task AssertInventoryDispatchBlockedAsync(SmokeGraph graph, string failureKind)
    {
        await using var mutationContext = _fixture.CreateDbContext();
        var state = await mutationContext.IngredientDispenserStates
            .Include(item => item.Device)
            .SingleAsync(item => item.Id == graph.DispenserStateId);
        var originalProfile = state.LevelToQuantityProfileJson;
        var originalDeviceStatus = state.Device.Status;
        var originalActive = state.IsActive;
        switch (failureKind)
        {
            case "inactive":
                state.IsActive = false;
                break;
            case "calibration":
                state.LevelToQuantityProfileJson = null;
                break;
            case "device":
                state.Device.SetStatus(DeviceStatus.Offline);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(failureKind));
        }
        await mutationContext.SaveChangesAsync();

        var orderId = await CreatePaidOrderAsync(graph);
        await using (var dispatchContext = _fixture.CreateDbContext())
        {
            var result = await new DispatchOrderExecutionCommandHandler(
                new OrderExecutionDispatchStore(dispatchContext),
                Options.Create(new OrderExecutionDispatchOptions()),
                new NoOpEdgeCommandWakeUpPublisher()).HandleAsync(new DispatchOrderExecutionCommand
                {
                    OrderId = orderId,
                    DispatchAttemptNo = 1
                });
            Assert.False(result.Succeeded);
            Assert.Equal(409, result.StatusCode);
        }

        state.IsActive = originalActive;
        state.LevelToQuantityProfileJson = originalProfile;
        state.Device.SetStatus(originalDeviceStatus);
        await mutationContext.SaveChangesAsync();
    }

    private async Task ReportProductionAsync(
        SmokeGraph graph,
        Guid commandId,
        Guid? productionJobId,
        long sequenceNumber,
        string status,
        Guid releaseId,
        string releaseChecksum,
        IReadOnlyCollection<StockMovementEvidenceInput>? stockMovements = null,
        int productionUnitNo = 1)
    {
        var result = await IngestProductionAsync(
            graph,
            commandId,
            productionJobId,
            sequenceNumber,
            status,
            releaseId,
            releaseChecksum,
            stockMovements,
            productionUnitNo);
        Assert.True(result.Succeeded, result.Message);
    }

    private async Task<Application.Shared.Wrappers.ApiResult<Application.EdgeIntegration.Reports.Results.ExecutionReportIngestResult>>
        IngestProductionAsync(
            SmokeGraph graph,
            Guid commandId,
            Guid? productionJobId,
            long sequenceNumber,
            string status,
            Guid releaseId,
            string releaseChecksum,
            IReadOnlyCollection<StockMovementEvidenceInput>? stockMovements = null,
            int productionUnitNo = 1)
    {
        await using var dbContext = _fixture.CreateDbContext();
        Guid? orderItemId = null;
        var normalizedStockMovements = stockMovements ?? [];
        if (productionJobId.HasValue)
        {
            var edgeCommand = await dbContext.EdgeCommands.AsNoTracking()
                .SingleAsync(candidate => candidate.Id == commandId);
            if (!edgeCommand.OrderId.HasValue)
                throw new InvalidOperationException("Production-job test command has no order identity.");
            var orderItem = await dbContext.OrderItems.AsNoTracking()
                .SingleAsync(candidate => candidate.OrderId == edgeCommand.OrderId.Value);
            orderItemId = orderItem.Id;
            normalizedStockMovements = normalizedStockMovements
                .Select(evidence => evidence.OrderItemId == Guid.Empty
                    ? evidence with { OrderItemId = orderItem.Id }
                    : evidence)
                .ToArray();
        }
        var reportStore = new ExecutionReportStore(dbContext);
        return await new IngestExecutionReportCommandHandler(
            reportStore,
            new NoOpRealtimeNotificationPublisher(),
            Options.Create(new ExecutionReportIngestionOptions()))
            .HandleAsync(new IngestExecutionReportCommand
            {
                KioskId = graph.KioskId,
                EndpointId = graph.EndpointId,
                CommandId = commandId,
                SourceEventId = Guid.NewGuid(),
                SequenceNumber = sequenceNumber,
                EdgeCreatedAt = DateTimeOffset.UtcNow,
                ReportType = "ProductionExecution",
                Status = status,
                SourceProductionJobId = productionJobId,
                OrderItemId = orderItemId,
                ProductionUnitNo = productionJobId.HasValue ? productionUnitNo : null,
                ProductionUnitQuantity = productionJobId.HasValue ? 1 : null,
                SourceConfigurationReleaseId = releaseId,
                ReleaseChecksum = releaseChecksum,
                PhysicalOutputMayHaveOccurred = status is "Running" or "Completed",
                StockMovements = normalizedStockMovements
            });
    }

    private async Task RefillAsync(
        SmokeGraph graph,
        CurrentUserContext user,
        decimal quantity,
        string reasonCode)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var result = await new RefillDispenserCommandHandler(
                new InventoryStore(dbContext),
                new NoOpRealtimeNotificationPublisher())
            .HandleAsync(new RefillDispenserCommand
            {
                KioskId = graph.KioskId,
                DispenserStateId = graph.DispenserStateId,
                UserContext = user,
                Quantity = quantity,
                ReasonCode = reasonCode
            });
        Assert.True(result.Succeeded, result.Message);
    }

    private static void AssertPostgresTimestampEqual(DateTimeOffset expected, DateTimeOffset actual)
    {
        Assert.InRange((expected - actual).Duration(), TimeSpan.Zero, TimeSpan.FromTicks(9));
    }

    private async Task PullAndAcceptAsync(SmokeGraph graph, Guid commandId)
    {
        await using (var dbContext = _fixture.CreateDbContext())
        {
            var pulled = await new PullEdgeCommandsCommandHandler(
                new EdgeCommandStore(dbContext),
                new ArtifactCommandPayloadEnricher(_fixture.CreateObjectStorage()))
                .HandleAsync(new PullEdgeCommandsCommand
                {
                    KioskId = graph.KioskId,
                    EndpointId = graph.EndpointId,
                    MaxCommands = 10
                });
            Assert.True(pulled.Succeeded, pulled.Message);
            var pulledCommand = Assert.Single(pulled.Data!.Commands, command => command.CommandId == commandId);
            if (pulledCommand.CommandType == EdgeCommandType.DeployConfiguration.ToString())
            {
                using var payload = JsonDocument.Parse(pulledCommand.PayloadJson);
                var bundle = payload.RootElement.GetProperty("FullEdgeBundle");
                Assert.EndsWith(".zip", bundle.GetProperty("StorageKey").GetString());
                Assert.False(string.IsNullOrWhiteSpace(bundle.GetProperty("DownloadUrl").GetString()));
                Assert.All(payload.RootElement.GetProperty("Artifacts").EnumerateArray(), artifact =>
                    Assert.False(string.IsNullOrWhiteSpace(artifact.GetProperty("DownloadUrl").GetString())));
            }
        }

        await using (var dbContext = _fixture.CreateDbContext())
        {
            var accepted = await new AcknowledgeEdgeCommandCommandHandler(
                new EdgeCommandStore(dbContext),
                new NoOpRealtimeNotificationPublisher())
                .HandleAsync(new AcknowledgeEdgeCommandCommand
                {
                    KioskId = graph.KioskId,
                    EndpointId = graph.EndpointId,
                    CommandId = commandId,
                    AckStatus = "Accepted",
                    AcknowledgedAt = DateTimeOffset.UtcNow
                });
            Assert.True(accepted.Succeeded, accepted.Message);
        }
    }

    private async Task ReportAsync(
        SmokeGraph graph,
        Guid commandId,
        Guid deploymentId,
        Guid sourceEventId,
        long sequenceNumber,
        string status)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var deployment = await dbContext.KioskConfigurationDeployments
            .AsNoTracking()
            .SingleAsync(x => x.Id == deploymentId);
        var reportStore = new ExecutionReportStore(dbContext);
        var result = await new IngestExecutionReportCommandHandler(
            reportStore,
            new NoOpRealtimeNotificationPublisher(),
            Options.Create(new ExecutionReportIngestionOptions()))
            .HandleAsync(new IngestExecutionReportCommand
            {
                KioskId = graph.KioskId,
                EndpointId = graph.EndpointId,
                CommandId = commandId,
                SourceEventId = sourceEventId,
                SequenceNumber = sequenceNumber,
                EdgeCreatedAt = DateTimeOffset.UtcNow,
                ReportType = "Deployment",
                Status = status,
                DeploymentId = deploymentId,
                SourceConfigurationReleaseId = deployment.ConfigurationReleaseId,
                ReleaseChecksum = deployment.ReleaseChecksum
            });
        Assert.True(result.Succeeded, result.Message);
    }

    private async Task AssertDeploymentProvenanceRejectedAsync(
        SmokeGraph graph,
        Guid commandId,
        Guid deploymentId)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var deployment = await dbContext.KioskConfigurationDeployments
            .AsNoTracking()
            .SingleAsync(x => x.Id == deploymentId);
        var result = await new IngestExecutionReportCommandHandler(
            new ExecutionReportStore(dbContext),
            new NoOpRealtimeNotificationPublisher(),
            Options.Create(new ExecutionReportIngestionOptions()))
            .HandleAsync(new IngestExecutionReportCommand
            {
                KioskId = graph.KioskId,
                EndpointId = graph.EndpointId,
                CommandId = commandId,
                SourceEventId = Guid.NewGuid(),
                SequenceNumber = 1,
                EdgeCreatedAt = DateTimeOffset.UtcNow,
                ReportType = "Deployment",
                Status = "Installed",
                DeploymentId = deploymentId,
                SourceConfigurationReleaseId = deployment.ConfigurationReleaseId,
                ReleaseChecksum = new string('f', 64)
            });

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("provenance", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<SmokeGraph> SeedPrerequisitesAsync()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var account = new Account
        {
            UserName = $"smoke-{Guid.NewGuid():N}",
            Email = $"smoke-{Guid.NewGuid():N}@example.test",
            Status = Domain.Identity.Enums.AccountStatus.Active
        };
        var organization = new Organization
        {
            Code = $"ORG-{Guid.NewGuid():N}",
            Name = "Operational smoke organization",
            Status = EntityStatus.Active
        };
        var store = new Store
        {
            OrganizationId = organization.Id,
            Code = $"STORE-{Guid.NewGuid():N}",
            Name = "Operational smoke store",
            Status = EntityStatus.Active
        };
        var kiosk = new Kiosk
        {
            OrganizationId = organization.Id,
            StoreId = store.Id,
            Code = $"KIOSK-{Guid.NewGuid():N}",
            Name = "Operational smoke kiosk",
            Status = KioskStatus.Active
        };
        var product = new Product
        {
            OrganizationId = organization.Id,
            ScopeType = TenantScopeType.Organization,
            Code = $"PRODUCT-{Guid.NewGuid():N}",
            Name = "Operational smoke product",
            BasePrice = 1
        };
        var variant = new ProductVariant
        {
            ProductId = product.Id,
            Product = product,
            Code = $"VARIANT-{Guid.NewGuid():N}",
            Name = "Operational smoke variant",
            BasePrice = 1,
            FulfillmentType = FulfillmentType.MachineProduced
        };
        var recipe = new Recipe
        {
            OrganizationId = organization.Id,
            ScopeType = TenantScopeType.Organization,
            ProductVariantId = variant.Id,
            ProductVariant = variant,
            Code = $"RECIPE-{Guid.NewGuid():N}",
            Name = "Operational smoke recipe",
            Status = RecipeStatus.Published
        };
        var deviceType = new DeviceType
        {
            Code = $"DISPENSER-{Guid.NewGuid():N}",
            Name = "Operational smoke dispenser"
        };
        dbContext.DeviceTypes.Add(deviceType);
        await dbContext.SaveChangesAsync();

        var device = Device.CreateProvisioning(
            deviceType.Id,
            null,
            kiosk.Id,
            $"DEVICE-{Guid.NewGuid():N}",
            "Operational smoke dispenser",
            null,
            null,
            null,
            null);
        device.DeviceType = deviceType;
        device.Kiosk = kiosk;
        device.SetStatus(DeviceStatus.Online);
        var ingredient = new Ingredient
        {
            Code = $"INGREDIENT-{Guid.NewGuid():N}",
            Name = "Operational smoke ingredient",
            Unit = "gram"
        };
        recipe.RecipeItems.Add(new RecipeItem
        {
            RecipeId = recipe.Id,
            Recipe = recipe,
            IngredientId = ingredient.Id,
            Ingredient = ingredient,
            Quantity = 10,
            Unit = "gram",
            StepOrder = 1,
            IsOptional = false
        });
        var dispenserState = new IngredientDispenserState
        {
            DeviceId = device.Id,
            Device = device,
            KioskId = kiosk.Id,
            Kiosk = kiosk,
            IngredientId = ingredient.Id,
            Ingredient = ingredient,
            ContainerCode = $"CONTAINER-{Guid.NewGuid():N}",
            CurrentLevelStatus = IngredientLevelStatus.Full,
            EstimatedQuantity = 100,
            CapacityQuantity = 100,
            Unit = "gram",
            LevelToQuantityProfileJson =
                """[{"Level":1,"EstimatedQuantity":10},{"Level":2,"EstimatedQuantity":50},{"Level":3,"EstimatedQuantity":100}]""",
            LastMeasuredAt = DateTimeOffset.UtcNow
        };
        var menu = new Menu
        {
            OrganizationId = organization.Id,
            StoreId = store.Id,
            KioskId = kiosk.Id,
            ScopeType = TenantScopeType.Kiosk,
            Code = $"MENU-{Guid.NewGuid():N}",
            Name = "Operational smoke menu",
            Status = MenuStatus.Active
        };
        var menuItem = new MenuItem
        {
            MenuId = menu.Id,
            Menu = menu,
            ProductId = product.Id,
            Product = product,
            ProductVariantId = variant.Id,
            ProductVariant = variant,
            RecipeId = recipe.Id,
            Recipe = recipe,
            Code = $"ITEM-{Guid.NewGuid():N}",
            DisplayName = "Operational smoke item",
            Status = MenuItemStatus.Active,
            Price = 1
        };
        var endpoint = KioskExecutionEndpoint.CreateProvisioning(
            kiosk.Id,
            $"EDGE-{Guid.NewGuid():N}",
            KioskExecutionProfile.FullEdge,
            ExecutionEndpointAuthenticationMode.MutualTls);
        endpoint.ReplaceSupportedRobotTargets([(RuntimeTargetCode, MachineModelCode, null)]);

        dbContext.AddRange(
            account,
            organization,
            store,
            kiosk,
            product,
            variant,
            recipe,
            menu,
            menuItem,
            device,
            ingredient,
            dispenserState,
            endpoint);
        await dbContext.SaveChangesAsync();

        var credential = endpoint.ProvisionCredential($"cert-{Guid.NewGuid():N}", DateTimeOffset.UtcNow);
        endpoint.Activate(Guid.NewGuid(), DateTimeOffset.UtcNow);
        dbContext.ExecutionEndpointCredentialBindings.Add(credential);
        var readiness = ExecutionEndpointReadinessProjection.Create(
            kiosk.Id, endpoint.Id, endpoint.FullEdgeRuntimeId!.Value, 1,
            ExecutionReadinessState.Ready, ExecutionActivityState.Idle, ExecutionSafetyState.Safe,
            null, PhysicalOutputState.No, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        dbContext.ExecutionEndpointReadinessProjections.Add(readiness);
        dbContext.ExecutionEndpointCapabilityProjections.Add(new ExecutionEndpointCapabilityProjection
        {
            ExecutionEndpointReadinessProjectionId = readiness.Id,
            CapabilityCode = "ICE_CREAM",
            IsAvailable = true
        });
        await dbContext.SaveChangesAsync();

        return new SmokeGraph(
            account.Id,
            organization.Id,
            store.Id,
            kiosk.Id,
            endpoint.Id,
            product.Id,
            variant.Id,
            recipe.Id,
            menuItem.Id,
            device.Id,
            ingredient.Id,
            dispenserState.Id,
            endpoint.FullEdgeRuntimeId!.Value);
    }

    private sealed record SmokeGraph(
        Guid AccountId,
        Guid OrganizationId,
        Guid StoreId,
        Guid KioskId,
        Guid EndpointId,
        Guid ProductId,
        Guid ProductVariantId,
        Guid RecipeId,
        Guid MenuItemId,
        Guid DeviceId,
        Guid IngredientId,
        Guid DispenserStateId,
        Guid SourceExecutorId);
}
