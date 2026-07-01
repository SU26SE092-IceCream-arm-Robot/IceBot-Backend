using Domain.Sync.Ingestion;
using Application.Abstractions.Realtime;
using Application.Devices;
using Application.Devices.Abstractions;
using Application.Devices.Commands;
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
