using System.Text.Json;
using Application.EdgeIntegration.Uplink;

namespace IceBot.UnitTests.EdgeIntegration;

public sealed class EdgeUplinkContractTests
{
    [Fact]
    public async Task Dispatcher_rejects_unknown_schema_before_touching_domain_handlers()
    {
        var dispatcher = new EdgeUplinkMessageDispatcher(
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
        var result = await dispatcher.DispatchAsync(
            Guid.NewGuid(),
            EdgeUplinkMessageTypes.Heartbeat,
            new EdgeUplinkEnvelope
            {
                SchemaVersion = 2,
                MessageId = Guid.NewGuid(),
                SentAt = DateTimeOffset.UtcNow,
                Payload = JsonDocument.Parse("{}").RootElement.Clone()
            },
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.False(result.Retryable);
    }

    [Fact]
    public void Message_type_catalog_is_explicit_and_stable()
    {
        Assert.Equal(
            new[]
            {
                "execution-report",
                "heartbeat",
                "inventory-observations",
                "production-events",
                "readiness",
                "reported-devices",
                "state-summaries",
                "telemetry-events"
            },
            EdgeUplinkMessageTypes.All.OrderBy(value => value, StringComparer.Ordinal));
    }

    [Fact]
    public void Serializer_rejects_unknown_fields_and_numeric_enums()
    {
        var options = EdgeUplinkJson.CreateSerializerOptions();

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<EdgeHeartbeatUplink>(
            """{"originNodeId":"00000000-0000-0000-0000-000000000001","status":0}""",
            options));
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<EdgeHeartbeatUplink>(
            """{"originNodeId":"00000000-0000-0000-0000-000000000001","unexpected":true}""",
            options));
    }

    [Fact]
    public void Reported_devices_contract_round_trips_edge_reported_hardware_snapshot()
    {
        var payload = new EdgeReportedDevicesUplink
        {
            SourceExecutorId = Guid.NewGuid(),
            SnapshotRevision = 1,
            ObservedAt = DateTimeOffset.UtcNow,
            Devices =
            [
                new EdgeReportedDeviceUplink
                {
                    SourceDeviceKey = "arm-left",
                    RuntimeTargetCode = "FAIRINO_LUA_V1",
                    MachineModelCode = "FR5"
                }
            ]
        };

        var json = JsonSerializer.Serialize(payload, EdgeUplinkJson.CreateSerializerOptions());
        var restored = JsonSerializer.Deserialize<EdgeReportedDevicesUplink>(json, EdgeUplinkJson.CreateSerializerOptions());

        var device = Assert.Single(restored!.Devices);
        Assert.Equal("arm-left", device.SourceDeviceKey);
        Assert.Equal("FAIRINO_LUA_V1", device.RuntimeTargetCode);
        Assert.Equal("FR5", device.MachineModelCode);
    }
}
