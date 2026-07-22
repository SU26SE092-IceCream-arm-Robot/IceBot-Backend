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
using Domain.Devices.Catalog;
using Domain.Devices.ExecutionEndpoints;
using Domain.Devices.Telemetry;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace IceBot.UnitTests.Devices;

public sealed class HeartbeatIngestionTests
{
    [Fact]
    public async Task HandleAsync_RejectsFutureTimestampBeforeStoreAccess()
    {
        var store = Substitute.For<IEdgeTelemetryIngestionStore>();
        var handler = new IngestKioskHeartbeatCommandHandler(
            store,
            Substitute.For<IRealtimeNotificationPublisher>(),
            Options.Create(new EdgeTelemetryIngestionOptions { MaxFutureClockSkewSeconds = 30 }));

        var result = await handler.HandleAsync(new IngestKioskHeartbeatCommand
        {
            KioskId = Guid.NewGuid(),
            EndpointId = Guid.NewGuid(),
            OriginNodeId = Guid.NewGuid(),
            HeartbeatSequence = 1,
            ReportedAt = DateTimeOffset.UtcNow.AddMinutes(2),
            Status = KioskHeartbeatStatus.Online
        });

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Heartbeat timestamp cannot exceed the allowed future clock skew.", result.Message);
        await store.DidNotReceive().GetEndpointAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
