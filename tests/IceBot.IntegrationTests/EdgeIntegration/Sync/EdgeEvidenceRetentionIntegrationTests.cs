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
public sealed class EdgeEvidenceRetentionIntegrationTests(IntegrationTestFixture fixture)
    : EdgeOperationalIntegrationTestBase(fixture)
{
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

}