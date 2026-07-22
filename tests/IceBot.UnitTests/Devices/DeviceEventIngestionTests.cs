using Application.Abstractions.Realtime;
using Application.Devices.Telemetry;
using Application.Devices.Catalog.Abstractions;
using Application.Devices.ExecutionEndpoints.Abstractions;
using Application.Devices.Telemetry.Abstractions;
using Application.Devices.Credentials.Abstractions;
using Application.Devices.Catalog.Commands;
using Application.Devices.ExecutionEndpoints.Commands;
using Application.Devices.Telemetry.Commands;
using Application.Devices.Connectivity.Commands;
using Application.Devices.Credentials.Commands;
using Application.Operations.Alerts.Notifications;
using Domain.Common.Enums;
using Microsoft.Extensions.Options;
using NSubstitute;
using Application.Devices.Telemetry.Rules;

namespace IceBot.UnitTests.Devices;

public sealed class DeviceEventIngestionTests
{
    [Fact]
    public void HistoricalEvent_IsNotEligibleForAlertAutomation()
    {
        var receivedAt = DateTimeOffset.UtcNow;

        Assert.False(DeviceEventAutomationRules.IsEligibleForAlertAutomation(
            receivedAt.AddHours(-2), receivedAt, 30));
        Assert.True(DeviceEventAutomationRules.IsEligibleForAlertAutomation(
            receivedAt.AddMinutes(-29), receivedAt, 30));
    }

    [Fact]
    public async Task HandleAsync_RejectsInformationalEvidenceBeforeStoreAccess()
    {
        var store = Substitute.For<IEdgeTelemetryIngestionStore>();
        var handler = new IngestDeviceEventCommandHandler(
            store,
            Substitute.For<IAlertIngestionStore>(),
            Substitute.For<IRealtimeNotificationPublisher>(),
            Substitute.For<ICriticalOperationalAlertNotifier>(),
            Options.Create(new EdgeTelemetryIngestionOptions()));

        var result = await handler.HandleAsync(new IngestDeviceEventCommand
        {
            KioskId = Guid.NewGuid(),
            EndpointId = Guid.NewGuid(),
            OriginNodeId = Guid.NewGuid(),
            DeviceId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            EventType = "MotorStatus",
            Severity = SeverityLevel.Info,
            Message = "Informational event",
            OccurredAt = DateTimeOffset.UtcNow
        });

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("Device event ingest accepts Warning, Error, or Critical evidence only.", result.Message);
        await store.DidNotReceive().GetEndpointAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
