using System.Buffers;
using System.Text;
using System.Text.Json;
using Application.EdgeIntegration.Uplink;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using IceBot.IntegrationTests.Infrastructure;
using Infrastructure.EdgeIntegration.Mqtt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using System.Net;
using System.Net.Sockets;

namespace IceBot.IntegrationTests.EdgeIntegration;

public sealed class MqttEdgeUplinkConsumerTests
{
    [IntegrationFact]
    public async Task Consumer_dispatches_endpoint_scoped_uplink_and_publishes_result()
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
        await broker.StartAsync();

        var endpointId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var dispatcher = new RecordingDispatcher();
        var services = new ServiceCollection()
            .AddSingleton<IEdgeUplinkMessageDispatcher>(dispatcher)
            .BuildServiceProvider();
        var options = Options.Create(new EdgeUplinkMqttOptions
        {
            Enabled = true,
            Host = broker.Hostname,
            Port = hostPort,
            ClientId = $"icebot-uplink-consumer-{Guid.NewGuid():N}",
            TopicPrefix = "icebot-test",
            ConsumerGroup = $"test-{Guid.NewGuid():N}",
            ConnectTimeoutSeconds = 2,
            PublishTimeoutSeconds = 2,
            ReconnectDelaySeconds = 1,
            MaxPayloadBytes = 64 * 1024,
            MaxConcurrentMessages = 2
        });
        using var consumer = new MqttEdgeUplinkConsumer(
            options,
            services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<MqttEdgeUplinkConsumer>.Instance);
        await consumer.StartAsync(CancellationToken.None);

        using var edgeClient = new MqttClientFactory().CreateMqttClient();
        var received = new TaskCompletionSource<EdgeUplinkResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        edgeClient.ApplicationMessageReceivedAsync += args =>
        {
            var result = JsonSerializer.Deserialize<EdgeUplinkResult>(
                Encoding.UTF8.GetString(args.ApplicationMessage.Payload.ToArray()),
                JsonOptions());
            if (result is not null)
                received.TrySetResult(result);
            return Task.CompletedTask;
        };
        await edgeClient.ConnectAsync(new MqttClientOptionsBuilder()
            .WithClientId(endpointId.ToString("D"))
            .WithTcpServer(broker.Hostname, hostPort)
            .Build());
        await edgeClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(filter => filter
                .WithTopic(EdgeUplinkTopic.Result("icebot-test", endpointId))
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
            .Build());

        var envelope = new EdgeUplinkEnvelope
        {
            MessageId = messageId,
            SentAt = DateTimeOffset.UtcNow,
            Payload = JsonDocument.Parse("""{"originNodeId":"00000000-0000-0000-0000-000000000001"}""")
                .RootElement.Clone()
        };
        var publish = new MqttApplicationMessageBuilder()
            .WithTopic($"icebot-test/execution-endpoints/{endpointId:D}/uplink/heartbeat")
            .WithPayload(JsonSerializer.Serialize(envelope, JsonOptions()))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (!received.Task.IsCompleted)
        {
            await edgeClient.PublishAsync(publish, timeout.Token);
            await Task.WhenAny(received.Task, Task.Delay(250, timeout.Token));
        }

        var response = await received.Task.WaitAsync(timeout.Token);
        Assert.True(response.Succeeded);
        Assert.Equal(messageId, response.MessageId);
        Assert.Equal(endpointId, response.EndpointId);
        Assert.Equal(EdgeUplinkMessageTypes.Heartbeat, dispatcher.MessageType);

        await consumer.StopAsync(CancellationToken.None);
        await edgeClient.DisconnectAsync();
        await services.DisposeAsync();
    }

    [Theory]
    [InlineData("icebot/execution-endpoints/98f3cd43-bd32-4450-955f-569f08cc412c/uplink/readiness", true)]
    [InlineData("icebot/execution-endpoints/not-a-guid/uplink/readiness", false)]
    [InlineData("icebot/execution-endpoints/98f3cd43-bd32-4450-955f-569f08cc412c/uplink/results", false)]
    [InlineData("other/execution-endpoints/98f3cd43-bd32-4450-955f-569f08cc412c/uplink/readiness", false)]
    public void Topic_parser_enforces_endpoint_ownership_shape(string topic, bool expected)
    {
        Assert.Equal(expected, EdgeUplinkTopic.TryParse("icebot", topic, out _, out _));
    }

    [Fact]
    public void Subscriptions_include_only_typed_uplink_and_exclude_results()
    {
        var subscriptions = EdgeUplinkTopic.Subscriptions("icebot", "cloud");

        Assert.Equal(EdgeUplinkMessageTypes.All.Count, subscriptions.Count);
        Assert.DoesNotContain(subscriptions, topic => topic.EndsWith("/uplink/+", StringComparison.Ordinal));
        Assert.DoesNotContain(subscriptions, topic => topic.EndsWith("/uplink/results", StringComparison.Ordinal));
        Assert.All(subscriptions, topic => Assert.StartsWith("$share/cloud/icebot/", topic));
    }

    private static JsonSerializerOptions JsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false
        };
        return options;
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private sealed class RecordingDispatcher : IEdgeUplinkMessageDispatcher
    {
        public string? MessageType { get; private set; }

        public Task<EdgeUplinkResult> DispatchAsync(
            Guid endpointId,
            string messageType,
            EdgeUplinkEnvelope envelope,
            CancellationToken cancellationToken)
        {
            MessageType = messageType;
            return Task.FromResult(new EdgeUplinkResult
            {
                MessageId = envelope.MessageId,
                EndpointId = endpointId,
                MessageType = messageType,
                ProcessedAt = DateTimeOffset.UtcNow,
                Succeeded = true,
                StatusCode = 200,
                Retryable = false,
                Message = "Accepted"
            });
        }
    }
}
