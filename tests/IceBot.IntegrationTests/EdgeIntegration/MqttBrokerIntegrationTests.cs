using Application.EdgeIntegration.Abstractions;
using Domain.Sync.Enums;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.EdgeIntegration.Mqtt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;

namespace IceBot.IntegrationTests.EdgeIntegration;

public sealed class MqttBrokerIntegrationTests
{
    [IntegrationFact]
    public async Task PublisherRecoversWhenMosquittoReturnsDuringRetryWindow()
    {
        var hostPort = GetAvailablePort();
        await using var broker = new ContainerBuilder("eclipse-mosquitto:2.0.22")
            .WithEntrypoint("/bin/sh")
            .WithCommand(
                "-c",
                "printf 'listener 1883 0.0.0.0\\nallow_anonymous true\\n' > /mosquitto/config/mosquitto.conf && exec /usr/sbin/mosquitto -c /mosquitto/config/mosquitto.conf")
            .WithPortBinding(hostPort, 1883)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(1883))
            .Build();
        var options = Options.Create(new EdgeCommandMqttOptions
        {
            Enabled = true,
            Host = broker.Hostname,
            Port = hostPort,
            ClientId = $"icebot-test-{Guid.NewGuid():N}",
            TopicPrefix = "icebot-test",
            ConnectTimeoutSeconds = 1,
            PublishTimeoutSeconds = 2,
            PublishRetryCount = 5,
            PublishRetryDelayMilliseconds = 1000
        });
        await using var transport = new MqttNetEdgeCommandTransport(options);
        var publisher = new MqttEdgeCommandWakeUpPublisher(
            options,
            transport,
            NullLogger<MqttEdgeCommandWakeUpPublisher>.Instance);

        var start = Task.Run(async () =>
        {
            await Task.Delay(500);
            await broker.StartAsync();
        });

        var published = await publisher.TryPublishAsync(new EdgeCommandWakeUp(
            Guid.NewGuid(),
            Guid.NewGuid(),
            EdgeCommandType.ExecuteOrder,
            DateTimeOffset.UtcNow));
        await start;

        Assert.True(published);
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
