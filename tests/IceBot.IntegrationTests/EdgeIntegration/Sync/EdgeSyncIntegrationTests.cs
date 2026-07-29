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
public sealed class EdgeSyncIntegrationTests(IntegrationTestFixture fixture)
    : EdgeOperationalIntegrationTestBase(fixture)
{
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

}