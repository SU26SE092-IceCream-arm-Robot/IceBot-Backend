using Domain.Sync.Ingestion;
using Domain.Devices.ExecutionEndpoints;
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
    public async Task HandleAsync_RejectsStockEvidenceWithoutProductionJobIdentity()
    {
        var receiptStore = Substitute.For<IExecutionReportReceiptStore>();
        var handler = new IngestExecutionReportCommandHandler(
            receiptStore,
            Substitute.For<IDeploymentReportStore>(),
            Substitute.For<IProductionExecutionReportStore>(),
            Substitute.For<IExecutionStockEvidenceStore>(),
            Substitute.For<IRealtimeNotificationPublisher>(),
            Options.Create(new ExecutionReportIngestionOptions()));

        var result = await handler.HandleAsync(new IngestExecutionReportCommand
        {
            KioskId = Guid.NewGuid(),
            EndpointId = Guid.NewGuid(),
            CommandId = Guid.NewGuid(),
            SourceEventId = Guid.NewGuid(),
            SequenceNumber = 1,
            EdgeCreatedAt = DateTimeOffset.UtcNow,
            ReportType = "ProductionExecution",
            Status = "Running",
            StockMovements =
            [
                new StockMovementEvidenceInput(
                    Guid.NewGuid(), Guid.NewGuid(), 1, null, DateTimeOffset.UtcNow, true)
            ]
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Stock movement evidence must be reported by a production job.", result.Message);
        await receiptStore.DidNotReceive().GetEndpointForReportAuthAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

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

        var receiptStore = Substitute.For<IExecutionReportReceiptStore>();
        var deploymentStore = Substitute.For<IDeploymentReportStore>();
        var productionStore = Substitute.For<IProductionExecutionReportStore>();
        var stockStore = Substitute.For<IExecutionStockEvidenceStore>();
        receiptStore.GetEndpointForReportAuthAsync(endpoint.Id, Arg.Any<CancellationToken>()).Returns(endpoint);
        receiptStore.ExecuteReportIngestionAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<Func<CancellationToken, Task<ApiResult<ExecutionReportIngestResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<ExecutionReportIngestResult>>>>()(CancellationToken.None));
        receiptStore.GetCommandAsync(edgeCommand.Id, Arg.Any<CancellationToken>()).Returns(edgeCommand);
        var handler = new IngestExecutionReportCommandHandler(
            receiptStore,
            deploymentStore,
            productionStore,
            stockStore,
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
        await productionStore.DidNotReceive().AddProductionExecutionRecordAsync(
            Arg.Any<Domain.ProductionExecution.Projections.ProductionExecutionRecord>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RejectsTimestampBeyondConfiguredFutureSkewBeforeStoreAccess()
    {
        var receiptStore = Substitute.For<IExecutionReportReceiptStore>();
        var handler = new IngestExecutionReportCommandHandler(
            receiptStore,
            Substitute.For<IDeploymentReportStore>(),
            Substitute.For<IProductionExecutionReportStore>(),
            Substitute.For<IExecutionStockEvidenceStore>(),
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
        await receiptStore.DidNotReceive().GetEndpointForReportAuthAsync(
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RejectsReusedSourceEventIdWithDifferentEnvelope()
    {
        var kioskId = Guid.NewGuid();
        var endpoint = ActiveFullEdgeEndpoint(kioskId);
        var sourceEventId = Guid.NewGuid();
        var receiptStore = Substitute.For<IExecutionReportReceiptStore>();
        receiptStore.GetEndpointForReportAuthAsync(endpoint.Id, Arg.Any<CancellationToken>()).Returns(endpoint);
        receiptStore.ExecuteReportIngestionAsync(
                endpoint.FullEdgeRuntimeId!.Value,
                sourceEventId,
                Arg.Any<Guid>(),
                Arg.Any<Func<CancellationToken, Task<ApiResult<ExecutionReportIngestResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<ExecutionReportIngestResult>>>>()(CancellationToken.None));
        receiptStore.GetSyncEventByEventIdAsync(endpoint.FullEdgeRuntimeId.Value, sourceEventId, Arg.Any<CancellationToken>())
            .Returns(new SyncEventInbox { EventId = sourceEventId, Status = SyncEventStatus.Processed });
        var handler = new IngestExecutionReportCommandHandler(
            receiptStore,
            Substitute.For<IDeploymentReportStore>(),
            Substitute.For<IProductionExecutionReportStore>(),
            Substitute.For<IExecutionStockEvidenceStore>(),
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

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("Execution report source event id was reused with different command or payload.", result.Message);
        await receiptStore.DidNotReceive().GetCommandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await receiptStore.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
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
