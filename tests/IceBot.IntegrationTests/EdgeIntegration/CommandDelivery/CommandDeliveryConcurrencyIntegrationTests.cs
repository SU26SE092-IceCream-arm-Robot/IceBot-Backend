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
public sealed class CommandDeliveryConcurrencyIntegrationTests(IntegrationTestFixture fixture)
    : EdgeOperationalIntegrationTestBase(fixture)
{
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
                    AcknowledgedAt = acknowledgedAt.AddSeconds(-2),
                    LocalStatePersisted = true
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
        Assert.True((await assertion.EdgeCommands.SingleAsync(command => command.Id == commandId)).RespondedAt!.Value >
            acknowledgedAt.AddSeconds(-2));
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

}