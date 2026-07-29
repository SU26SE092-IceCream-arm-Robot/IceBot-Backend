using System.Text.Json;
using Application.EdgeIntegration.Abstractions;
using Domain.Sync.Enums;
using Infrastructure.EdgeIntegration.Mqtt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IceBot.IntegrationTests.EdgeIntegration;

public sealed class MqttEdgeCommandWakeUpPublisherTests
{
    [Fact]
    public async Task BrokerTimeoutThenRecoveryPublishesSuccessfully()
    {
        var transport = new ScriptedTransport(
            _ => throw new TimeoutException("broker timeout"),
            _ => Task.CompletedTask);
        var publisher = CreatePublisher(transport, retryCount: 1);

        var result = await publisher.TryPublishAsync(CreateNotification());

        Assert.True(result);
        Assert.Equal(2, transport.Payloads.Count);
    }

    [Fact]
    public async Task RetryKeepsTheSameCommandId()
    {
        var notification = CreateNotification();
        var transport = new ScriptedTransport(
            _ => throw new TimeoutException("broker timeout"),
            _ => Task.CompletedTask);
        var publisher = CreatePublisher(transport, retryCount: 1);

        Assert.True(await publisher.TryPublishAsync(notification));

        var commandIds = transport.Payloads
            .Select(payload => JsonDocument.Parse(payload).RootElement.GetProperty("CommandId").GetGuid())
            .ToArray();
        Assert.Equal([notification.CommandId, notification.CommandId], commandIds);
    }

    [Fact]
    public async Task ExhaustedRetryReturnsFalseWithoutMutatingTheNotification()
    {
        var notification = CreateNotification();
        var transport = new ScriptedTransport(
            _ => throw new TimeoutException("broker timeout"),
            _ => throw new TimeoutException("broker timeout"));
        var publisher = CreatePublisher(transport, retryCount: 1);

        var result = await publisher.TryPublishAsync(notification);

        Assert.False(result);
        Assert.Equal(2, transport.Payloads.Count);
        Assert.All(transport.Payloads, payload =>
            Assert.Equal(notification.CommandId, JsonDocument.Parse(payload).RootElement.GetProperty("CommandId").GetGuid()));
    }

    [Fact]
    public async Task CallerCancellationStopsMqttRetryImmediately()
    {
        using var cancellation = new CancellationTokenSource();
        var attempts = 0;
        var transport = new ScriptedTransport(_ =>
        {
            attempts++;
            cancellation.Cancel();
            throw new OperationCanceledException(cancellation.Token);
        });
        var publisher = CreatePublisher(transport, retryCount: 3);

        var result = await publisher.TryPublishAsync(CreateNotification(), cancellation.Token);

        Assert.False(result);
        Assert.Equal(1, attempts);
    }

    private static MqttEdgeCommandWakeUpPublisher CreatePublisher(
        IMqttEdgeCommandTransport transport,
        int retryCount)
    {
        return new MqttEdgeCommandWakeUpPublisher(
            Options.Create(new EdgeCommandMqttOptions
            {
                Enabled = true,
                TopicPrefix = "icebot",
                PublishRetryCount = retryCount,
                PublishRetryDelayMilliseconds = 1,
                PublishTimeoutSeconds = 2
            }),
            transport,
            NullLogger<MqttEdgeCommandWakeUpPublisher>.Instance);
    }

    private static EdgeCommandWakeUp CreateNotification() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        EdgeCommandType.ExecuteOrder,
        DateTimeOffset.UtcNow);

    private sealed class ScriptedTransport(params Func<string, Task>[] outcomes) : IMqttEdgeCommandTransport
    {
        private int _attempt;

        public List<string> Payloads { get; } = [];

        public async Task PublishAsync(
            string topic,
            string payload,
            CancellationToken cancellationToken = default)
        {
            Payloads.Add(payload);
            await outcomes[Math.Min(_attempt++, outcomes.Length - 1)](payload);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
