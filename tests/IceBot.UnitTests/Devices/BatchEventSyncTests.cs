using Domain.Sync.Ingestion;
using Application.Abstractions.Realtime;
using Application.Devices.Telemetry;
using Application.Devices.Catalog.Abstractions;
using Application.Devices.ExecutionEndpoints.Abstractions;
using Application.Devices.Telemetry.Abstractions;
using Application.Devices.Connectivity.Abstractions;
using Application.Devices.Credentials.Abstractions;
using Application.Devices.Catalog.Commands;
using Application.Devices.ExecutionEndpoints.Commands;
using Application.Devices.Telemetry.Commands;
using Application.Devices.Connectivity.Commands;
using Application.Devices.Credentials.Commands;
using Domain.Sync.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace IceBot.UnitTests.Devices;

public sealed class BatchEventSyncTests
{
    [Fact]
    public async Task ExistingReceipt_ReturnsDuplicateWithoutDispatchingTypedHandler()
    {
        var receiptStore = Substitute.For<IBatchEventSyncStore>();
        var telemetryStore = Substitute.For<IEdgeTelemetryIngestionStore>();
        var publisher = Substitute.For<IRealtimeNotificationPublisher>();
        var options = Options.Create(new EdgeTelemetryIngestionOptions());
        var eventId = Guid.NewGuid();
        receiptStore.GetReceiptAsync(Arg.Any<Guid>(), eventId, Arg.Any<CancellationToken>())
            .Returns(new SyncEventInbox { EventId = eventId });
        var handler = new IngestBatchEventsCommandHandler(
            receiptStore,
            new IngestKioskHeartbeatCommandHandler(telemetryStore, publisher, options),
            new IngestDeviceEventCommandHandler(telemetryStore, Substitute.For<IAlertIngestionStore>(), publisher, options),
            new IngestLocalOperationLogCommandHandler(telemetryStore, options),
            options,
            NullLogger<IngestBatchEventsCommandHandler>.Instance);

        var result = await handler.HandleAsync(new IngestBatchEventsCommand
        {
            KioskId = Guid.NewGuid(),
            EndpointId = Guid.NewGuid(),
            OriginNodeId = Guid.NewGuid(),
            Events =
            [
                new BatchSyncEventItem
                {
                    EventId = eventId,
                    EventType = BatchSyncEventType.Heartbeat,
                    Heartbeat = new BatchHeartbeatData { HeartbeatSequence = 1, ReportedAt = DateTimeOffset.UtcNow }
                }
            ]
        });

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Data!.DuplicateCount);
        await telemetryStore.DidNotReceive().GetEndpointAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
