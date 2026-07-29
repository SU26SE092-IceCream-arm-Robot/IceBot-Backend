using Domain.Sync.Ingestion;
using Domain.Devices.ExecutionEndpoints;
using Application.Devices.Telemetry;
using Application.Devices.Catalog.Abstractions;
using Application.Devices.ExecutionEndpoints.Abstractions;
using Application.Devices.Telemetry.Abstractions;
using Application.Sync.Ingestion.Abstractions;
using Application.Devices.Connectivity.Abstractions;
using Application.Devices.Credentials.Abstractions;
using Application.Devices.Catalog.Commands;
using Application.Devices.ExecutionEndpoints.Commands;
using Application.Sync.Ingestion.Commands;
using Application.Sync.Ingestion.Results;
using Application.Devices.Connectivity.Commands;
using Application.Devices.Credentials.Commands;
using Domain.Devices.Catalog;
using Domain.Devices.Telemetry;
using Domain.Sync.Entities;
using Domain.Sync.Enums;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Text.Json;

namespace IceBot.UnitTests.Sync;

public sealed class ProductionSyncProtocolTests
{
    [Fact]
    public void Checkpoint_AdvancesOnlyContiguously()
    {
        var checkpoint = ProductionEventCheckpoint.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Assert.Throws<Domain.Common.DomainRuleException>(() => checkpoint.AdvanceTo(2, Guid.NewGuid()));

        checkpoint.AdvanceTo(1, Guid.NewGuid());
        Assert.Equal(1, checkpoint.LastContiguousSequenceNumber);
    }

    [Fact]
    public void StateSummary_AppliesOnlyNewerRevision()
    {
        var now = DateTimeOffset.UtcNow;
        var summary = EdgeStateSummary.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Execution", 2, 1, now, now, "{}");

        Assert.Throws<Domain.Common.DomainRuleException>(() =>
            summary.ApplyNewerRevision(2, 1, now, now, "{}"));

        summary.ApplyNewerRevision(3, 1, now.AddSeconds(1), now.AddSeconds(1), "{\"status\":\"Completed\"}");
        Assert.Equal(3, summary.StateRevision);
    }

    [Fact]
    public async Task ProductionEvent_WithGap_IsStoredWithoutAdvancingCheckpoint()
    {
        var kioskId = Guid.NewGuid();
        var endpoint = ActiveEndpoint(kioskId);
        var sourceExecutorId = endpoint.FullEdgeRuntimeId!.Value;
        var eventId = Guid.NewGuid();
        var telemetryStore = Substitute.For<IEdgeTelemetryIngestionStore>();
        var syncStore = Substitute.For<IProductionEventSyncStore>();
        telemetryStore.GetEndpointAsync(endpoint.Id, Arg.Any<CancellationToken>()).Returns(endpoint);
        syncStore.ExecuteHistoryIngestionAsync(
                sourceExecutorId,
                Arg.Any<Func<CancellationToken, Task<Application.Shared.Wrappers.ApiResult<ProductionEventSyncResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<Application.Shared.Wrappers.ApiResult<ProductionEventSyncResult>>>>()(CancellationToken.None));
        syncStore.ListContiguousCandidatesAsync(sourceExecutorId, 0, Arg.Any<CancellationToken>())
            .Returns([new SyncEventInbox { EventId = eventId, SourceNodeId = sourceExecutorId, SequenceNumber = 2 }]);

        var handler = new IngestProductionEventCommandHandler(
            telemetryStore, syncStore, Options.Create(new EdgeTelemetryIngestionOptions()));
        var result = await handler.HandleAsync(new IngestProductionEventCommand
        {
            KioskId = kioskId,
            EndpointId = endpoint.Id,
            SourceExecutorId = sourceExecutorId,
            EventId = eventId,
            SequenceNumber = 2,
            EventType = "ProductionStarted",
            SchemaVersion = 1,
            EdgeCreatedAt = DateTimeOffset.UtcNow,
            PayloadJson = "{}"
        });

        Assert.True(result.Succeeded, result.Message);
        Assert.Equal(0, result.Data!.AcknowledgedSequenceNumber);
        await syncStore.Received(1).AddEventAsync(Arg.Any<SyncEventInbox>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProductionEvent_RejectsReusedEventIdWithDifferentPayloadEnvelope()
    {
        var kioskId = Guid.NewGuid();
        var endpoint = ActiveEndpoint(kioskId);
        var sourceExecutorId = endpoint.FullEdgeRuntimeId!.Value;
        var eventId = Guid.NewGuid();
        var productionJobId = Guid.NewGuid();
        var telemetryStore = Substitute.For<IEdgeTelemetryIngestionStore>();
        var syncStore = Substitute.For<IProductionEventSyncStore>();
        telemetryStore.GetEndpointAsync(endpoint.Id, Arg.Any<CancellationToken>()).Returns(endpoint);
        syncStore.ExecuteHistoryIngestionAsync(
                sourceExecutorId,
                Arg.Any<Func<CancellationToken, Task<Application.Shared.Wrappers.ApiResult<ProductionEventSyncResult>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<Application.Shared.Wrappers.ApiResult<ProductionEventSyncResult>>>>()(CancellationToken.None));
        syncStore.GetEventByIdAsync(sourceExecutorId, eventId, Arg.Any<CancellationToken>())
            .Returns(new SyncEventInbox
            {
                EventId = eventId,
                KioskId = kioskId,
                SourceNodeId = sourceExecutorId,
                SequenceNumber = 1,
                EventType = "ProductionStarted",
                AggregateType = "ProductionJob",
                AggregateId = productionJobId,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    schemaVersion = 1,
                    orderId = (Guid?)null,
                    sourceCommandId = (Guid?)null,
                    productionJobId,
                    payload = JsonSerializer.Deserialize<JsonElement>("{\"step\":1}")
                }),
                Status = SyncEventStatus.Processed
            });

        var handler = new IngestProductionEventCommandHandler(
            telemetryStore, syncStore, Options.Create(new EdgeTelemetryIngestionOptions()));
        var result = await handler.HandleAsync(new IngestProductionEventCommand
        {
            KioskId = kioskId,
            EndpointId = endpoint.Id,
            SourceExecutorId = sourceExecutorId,
            EventId = eventId,
            SequenceNumber = 1,
            EventType = "ProductionStarted",
            SchemaVersion = 1,
            ProductionJobId = productionJobId,
            EdgeCreatedAt = DateTimeOffset.UtcNow,
            PayloadJson = "{\"step\":2}"
        });

        Assert.False(result.Succeeded);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("Production event id was reused with a different identity or payload envelope.", result.Message);
        await syncStore.DidNotReceive().AddEventAsync(Arg.Any<SyncEventInbox>(), Arg.Any<CancellationToken>());
    }

    private static KioskExecutionEndpoint ActiveEndpoint(Guid kioskId)
    {
        var now = DateTimeOffset.UtcNow;
        var endpoint = KioskExecutionEndpoint.CreateProvisioning(
            kioskId, "EDGE-SYNC", KioskExecutionProfile.FullEdge, ExecutionEndpointAuthenticationMode.MutualTls);
        endpoint.ProvisionCredential("certificate-fingerprint", now);
        endpoint.Activate(Guid.NewGuid(), now);
        return endpoint;
    }
}
