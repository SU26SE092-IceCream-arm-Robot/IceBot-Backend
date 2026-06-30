using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration;
using Application.EdgeIntegration.Commands;
using Application.EdgeIntegration.Results;
using Application.Shared.Wrappers;
using Domain.Devices.Entities;
using Domain.Devices.Enums;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using NSubstitute;
using Application.Abstractions.Realtime;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IceBot.UnitTests.EdgeIntegration;

public sealed class ExecutionReportIngestionTests
{
    [Fact]
    public async Task HandleAsync_RejectsProductionReleaseThatDiffersFromAcceptedCommandPayload()
    {
        var now = DateTimeOffset.UtcNow;
        var kioskId = Guid.NewGuid();
        var endpoint = ActiveFullEdgeEndpoint(kioskId);
        var dispatchedReleaseId = Guid.NewGuid();
        var dispatchedChecksum = new string('a', 64);
        var edgeCommand = EdgeCommand.Create(
            EdgeCommandType.ExecuteOrder,
            kioskId,
            endpoint.Id,
            JsonSerializer.Serialize(new
            {
                ConfigurationReleaseId = dispatchedReleaseId,
                ReleaseChecksum = dispatchedChecksum
            }),
            now,
            Guid.NewGuid(),
            1,
            now.AddMinutes(30));
        edgeCommand.Id = Guid.NewGuid();
        edgeCommand.RecordDeliveryAttempt(1, now, EdgeCommandDeliveryOutcome.Sent);
        edgeCommand.Accept(now);

        var store = Substitute.For<IExecutionReportStore>();
        store.GetEndpointForReportAuthAsync(endpoint.Id, Arg.Any<CancellationToken>()).Returns(endpoint);
        store.ExecuteReportIngestionAsync(
                Arg.Any<Guid>(),
                Arg.Any<Func<CancellationToken, Task<ApiResult<ExecutionReportIngestResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<ExecutionReportIngestResult>>>>()(CancellationToken.None));
        store.GetCommandAsync(edgeCommand.Id, Arg.Any<CancellationToken>()).Returns(edgeCommand);
        var handler = new IngestExecutionReportCommandHandler(
            store,
            Substitute.For<IRealtimeNotificationPublisher>(),
            Options.Create(new ExecutionReportIngestionOptions()));

        var result = await handler.HandleAsync(new IngestExecutionReportCommand
        {
            KioskId = kioskId,
            EndpointId = endpoint.Id,
            CommandId = edgeCommand.Id,
            SourceEventId = Guid.NewGuid(),
            SequenceNumber = 1,
            EdgeCreatedAt = now,
            ReportType = "ProductionExecution",
            Status = "Running",
            SourceConfigurationReleaseId = dispatchedReleaseId,
            ReleaseChecksum = new string('b', 64)
        });

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Production execution report release does not match the dispatched command.", result.Message);
        await store.DidNotReceive().AddProductionExecutionRecordAsync(
            Arg.Any<Domain.ProductionExecution.Projections.ProductionExecutionRecord>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RejectsTimestampBeyondConfiguredFutureSkewBeforeStoreAccess()
    {
        var store = Substitute.For<IExecutionReportStore>();
        var handler = new IngestExecutionReportCommandHandler(
            store,
            Substitute.For<IRealtimeNotificationPublisher>(),
            Options.Create(new ExecutionReportIngestionOptions { MaxFutureClockSkewSeconds = 30 }));

        var result = await handler.HandleAsync(new IngestExecutionReportCommand
        {
            KioskId = Guid.NewGuid(),
            EndpointId = Guid.NewGuid(),
            CommandId = Guid.NewGuid(),
            SourceEventId = Guid.NewGuid(),
            SequenceNumber = 1,
            EdgeCreatedAt = DateTimeOffset.UtcNow.AddMinutes(2),
            ReportType = "ProductionExecution",
            Status = "Running"
        });

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Execution report timestamps cannot exceed the allowed future clock skew.", result.Message);
        await store.DidNotReceive().GetEndpointForReportAuthAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ReturnsDuplicateWithoutReapplyingExistingSourceEvent()
    {
        var kioskId = Guid.NewGuid();
        var endpoint = ActiveFullEdgeEndpoint(kioskId);
        var sourceEventId = Guid.NewGuid();
        var store = Substitute.For<IExecutionReportStore>();
        store.GetEndpointForReportAuthAsync(endpoint.Id, Arg.Any<CancellationToken>()).Returns(endpoint);
        store.ExecuteReportIngestionAsync(
                sourceEventId,
                Arg.Any<Func<CancellationToken, Task<ApiResult<ExecutionReportIngestResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<ExecutionReportIngestResult>>>>()(CancellationToken.None));
        store.GetSyncEventByEventIdAsync(sourceEventId, Arg.Any<CancellationToken>())
            .Returns(new SyncEventInbox { EventId = sourceEventId });
        var handler = new IngestExecutionReportCommandHandler(
            store,
            Substitute.For<IRealtimeNotificationPublisher>(),
            Options.Create(new ExecutionReportIngestionOptions()));

        var result = await handler.HandleAsync(new IngestExecutionReportCommand
        {
            KioskId = kioskId,
            EndpointId = endpoint.Id,
            CommandId = Guid.NewGuid(),
            SourceEventId = sourceEventId,
            SequenceNumber = 1,
            EdgeCreatedAt = DateTimeOffset.UtcNow,
            ReportType = "Deployment",
            Status = "Installed",
            DeploymentId = Guid.NewGuid()
        });

        Assert.True(result.Succeeded);
        Assert.True(result.Data!.Duplicate);
        Assert.False(result.Data.Applied);
        await store.DidNotReceive().GetCommandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static KioskExecutionEndpoint ActiveFullEdgeEndpoint(Guid kioskId)
    {
        var now = DateTimeOffset.UtcNow;
        var endpoint = KioskExecutionEndpoint.CreateProvisioning(
            kioskId,
            "EDGE-01",
            KioskExecutionProfile.FullEdge,
            ExecutionEndpointAuthenticationMode.MutualTls);
        endpoint.ProvisionCredential("certificate-fingerprint", now);
        endpoint.Activate(Guid.NewGuid(), now);
        return endpoint;
    }
}
