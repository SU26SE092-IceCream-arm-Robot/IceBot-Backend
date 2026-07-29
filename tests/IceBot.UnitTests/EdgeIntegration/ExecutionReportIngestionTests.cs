using Domain.Sync.Ingestion;
using Domain.Devices.ExecutionEndpoints;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration;
using Application.EdgeIntegration.Dispatch;
using Application.EdgeIntegration.Reports;
using Application.EdgeIntegration.CommandDelivery.Commands;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.EdgeIntegration.Reports.Commands;
using Application.EdgeIntegration.Timeouts.Commands;
using Application.EdgeIntegration.CommandDelivery.Results;
using Application.EdgeIntegration.Dispatch.Results;
using Application.EdgeIntegration.Reports.Results;
using Application.Shared.Wrappers;
using Domain.Devices.Catalog;
using Domain.Devices.Telemetry;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using NSubstitute;
using Application.Abstractions.Realtime;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Nodes;
using Application.EdgeIntegration.Dispatch.Contracts;
using Application.EdgeIntegration.Reports.Contracts;

namespace IceBot.UnitTests.EdgeIntegration;

public sealed class ExecutionReportIngestionTests
{
    [Fact]
    public async Task HandleAsync_RejectsRestartRecoveryWithoutProductionJobIdentity()
    {
        var unitOfWork = Substitute.For<IExecutionReportUnitOfWork>();
        var handler = new IngestExecutionReportCommandHandler(
            unitOfWork,
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
            Status = "RequiresManualIntervention",
            ErrorCode = ExecutionInterruptionCodes.ControllerRestarted
        });

        Assert.False(result.Succeeded);
        Assert.Equal(
            "Runtime, controller, and power interruption reports must identify a production job and require manual intervention.",
            result.Message);
        await unitOfWork.DidNotReceive().GetEndpointForReportAuthAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RejectsManualInterventionWithoutReasonCode()
    {
        var unitOfWork = Substitute.For<IExecutionReportUnitOfWork>();
        var handler = new IngestExecutionReportCommandHandler(
            unitOfWork,
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
            Status = "RequiresManualIntervention"
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Manual-intervention reports require an error code.", result.Message);
    }

    [Fact]
    public async Task HandleAsync_RejectsLocalPersistenceFailureWithoutProductionJobIdentity()
    {
        var unitOfWork = Substitute.For<IExecutionReportUnitOfWork>();
        var handler = new IngestExecutionReportCommandHandler(
            unitOfWork,
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
            Status = "RequiresManualIntervention",
            ErrorCode = ExecutionPersistenceFailureCodes.LocalPersistenceLost
        });

        Assert.False(result.Succeeded);
        Assert.Equal(
            "Local persistence failure reports must identify a production job and require manual intervention.",
            result.Message);
    }

    [Fact]
    public async Task HandleAsync_RejectsStockEvidenceWithoutProductionJobIdentity()
    {
        var receiptStore = Substitute.For<IExecutionReportUnitOfWork>();
        var handler = new IngestExecutionReportCommandHandler(
            receiptStore,
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
    public async Task HandleAsync_RejectsStockEvidenceForDifferentOrderItem()
    {
        var unitOfWork = Substitute.For<IExecutionReportUnitOfWork>();
        var handler = new IngestExecutionReportCommandHandler(
            unitOfWork,
            Substitute.For<IRealtimeNotificationPublisher>(),
            Options.Create(new ExecutionReportIngestionOptions()));
        var reportedOrderItemId = Guid.NewGuid();

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
            SourceProductionJobId = Guid.NewGuid(),
            OrderItemId = reportedOrderItemId,
            ProductionUnitNo = 1,
            ProductionUnitQuantity = 1,
            StockMovements =
            [
                new StockMovementEvidenceInput(
                    Guid.NewGuid(), Guid.NewGuid(), 1, null, DateTimeOffset.UtcNow, true, Guid.NewGuid())
            ]
        });

        Assert.False(result.Succeeded);
        Assert.Equal(
            "Stock movement evidence must belong to the reported production-job order item.",
            result.Message);
        await unitOfWork.DidNotReceive().GetEndpointForReportAuthAsync(
            Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RejectsFailedProductionReportWithoutPhysicalOutputEvidence()
    {
        var unitOfWork = Substitute.For<IExecutionReportUnitOfWork>();
        var handler = new IngestExecutionReportCommandHandler(
            unitOfWork,
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
            Status = "Failed"
        });

        Assert.False(result.Succeeded);
        Assert.Equal(
            "Failed production execution reports must state whether physical output may have occurred.",
            result.Message);
        await unitOfWork.DidNotReceive().GetEndpointForReportAuthAsync(
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

        var receiptStore = Substitute.For<IExecutionReportUnitOfWork>();
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
        await receiptStore.DidNotReceive().AddProductionExecutionRecordAsync(
            Arg.Any<Domain.ProductionExecution.Projections.ProductionExecutionRecord>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RejectsActiveArtifactSetThatDiffersFromAcceptedCommandPayload()
    {
        var now = DateTimeOffset.UtcNow;
        var kioskId = Guid.NewGuid();
        var endpoint = ActiveFullEdgeEndpoint(kioskId);
        var releaseId = Guid.NewGuid();
        var releaseChecksum = new string('a', 64);
        var edgeCommand = EdgeCommand.Create(
            EdgeCommandType.ExecuteOrder,
            kioskId,
            endpoint.Id,
            JsonSerializer.Serialize(new
            {
                ConfigurationReleaseId = releaseId,
                ReleaseChecksum = releaseChecksum,
                ActiveSetVersion = 7,
                ActiveSetChecksum = new string('b', 64)
            }),
            now,
            Guid.NewGuid(),
            1,
            now.AddMinutes(30));
        edgeCommand.Id = Guid.NewGuid();
        edgeCommand.RecordDeliveryAttempt(1, now, EdgeCommandDeliveryOutcome.Sent);
        edgeCommand.Accept(now);

        var unitOfWork = Substitute.For<IExecutionReportUnitOfWork>();
        unitOfWork.GetEndpointForReportAuthAsync(endpoint.Id, Arg.Any<CancellationToken>()).Returns(endpoint);
        unitOfWork.ExecuteReportIngestionAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
                Arg.Any<Func<CancellationToken, Task<ApiResult<ExecutionReportIngestResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<ExecutionReportIngestResult>>>>()(
                CancellationToken.None));
        unitOfWork.GetCommandAsync(edgeCommand.Id, Arg.Any<CancellationToken>()).Returns(edgeCommand);
        var handler = new IngestExecutionReportCommandHandler(
            unitOfWork,
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
            SourceConfigurationReleaseId = releaseId,
            ReleaseChecksum = releaseChecksum,
            ActiveSetVersion = 8,
            ActiveSetChecksum = new string('b', 64)
        });

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(
            "Production execution report active artifact set does not match the dispatched command.",
            result.Message);
    }

    [Fact]
    public async Task HandleAsync_RejectsTimestampBeyondConfiguredFutureSkewBeforeStoreAccess()
    {
        var receiptStore = Substitute.For<IExecutionReportUnitOfWork>();
        var handler = new IngestExecutionReportCommandHandler(
            receiptStore,
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
        var receiptStore = Substitute.For<IExecutionReportUnitOfWork>();
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

    [Fact]
    public async Task HandleAsync_TreatsReorderedCanonicalInboxPayloadAsDuplicate()
    {
        var now = DateTimeOffset.UtcNow;
        var kioskId = Guid.NewGuid();
        var endpoint = ActiveFullEdgeEndpoint(kioskId);
        var sourceEventId = Guid.NewGuid();
        var commandId = Guid.NewGuid();
        var payload = new ExecutionReportInboxPayload
        {
            CommandId = commandId,
            ReportType = "Deployment",
            Status = "Installed",
            SequenceNumber = 1,
            EdgeCreatedAt = now,
            DeploymentId = Guid.NewGuid()
        };
        var node = JsonNode.Parse(JsonSerializer.Serialize(payload))!.AsObject();
        var reordered = new JsonObject(node.Reverse().Select(property =>
            KeyValuePair.Create(property.Key, property.Value?.DeepClone())));

        var unitOfWork = Substitute.For<IExecutionReportUnitOfWork>();
        unitOfWork.GetEndpointForReportAuthAsync(endpoint.Id, Arg.Any<CancellationToken>()).Returns(endpoint);
        unitOfWork.ExecuteReportIngestionAsync(
                endpoint.FullEdgeRuntimeId!.Value, sourceEventId, commandId,
                Arg.Any<Func<CancellationToken, Task<ApiResult<ExecutionReportIngestResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<ApiResult<ExecutionReportIngestResult>>>>()(CancellationToken.None));
        unitOfWork.GetSyncEventByEventIdAsync(endpoint.FullEdgeRuntimeId.Value, sourceEventId, Arg.Any<CancellationToken>())
            .Returns(new SyncEventInbox
            {
                EventId = sourceEventId,
                KioskId = kioskId,
                SourceNodeId = endpoint.FullEdgeRuntimeId.Value,
                CausationId = commandId,
                EventType = "ExecutionReport.Deployment",
                AggregateType = "EdgeCommand",
                AggregateId = commandId,
                PayloadJson = reordered.ToJsonString(),
                Status = SyncEventStatus.Processed
            });
        var handler = new IngestExecutionReportCommandHandler(
            unitOfWork,
            Substitute.For<IRealtimeNotificationPublisher>(),
            Options.Create(new ExecutionReportIngestionOptions()));

        var result = await handler.HandleAsync(new IngestExecutionReportCommand
        {
            KioskId = kioskId,
            EndpointId = endpoint.Id,
            CommandId = commandId,
            SourceEventId = sourceEventId,
            SequenceNumber = 1,
            EdgeCreatedAt = now,
            ReportType = "Deployment",
            Status = "Installed",
            DeploymentId = payload.DeploymentId
        });

        Assert.True(result.Succeeded, result.Message);
        Assert.True(result.Data!.Duplicate);
        await unitOfWork.DidNotReceive().GetCommandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
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
