using System.Buffers;
using System.Diagnostics;
using System.Text.Json;
using Application.EdgeIntegration.Observability;
using Application.EdgeIntegration.Uplink;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;

namespace Infrastructure.EdgeIntegration.Mqtt;

public sealed class MqttEdgeUplinkConsumer : BackgroundService
{
    private readonly EdgeUplinkMqttOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MqttEdgeUplinkConsumer> _logger;
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _clientOptions;
    private readonly SemaphoreSlim _messageSlots;
    private readonly JsonSerializerOptions _serializerOptions;
    private CancellationToken _stoppingToken;

    public MqttEdgeUplinkConsumer(
        IOptions<EdgeUplinkMqttOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<MqttEdgeUplinkConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _client = new MqttClientFactory().CreateMqttClient();
        _messageSlots = new SemaphoreSlim(_options.MaxConcurrentMessages, _options.MaxConcurrentMessages);
        _serializerOptions = EdgeUplinkJson.CreateSerializerOptions();

        var builder = new MqttClientOptionsBuilder()
            .WithClientId(_options.ClientId)
            .WithTcpServer(_options.Host, _options.Port);
        if (!string.IsNullOrWhiteSpace(_options.Username))
            builder.WithCredentials(_options.Username, _options.Password);
        if (_options.UseTls)
            builder.WithTlsOptions(tls => tls.UseTls());
        _clientOptions = builder.Build();
        _client.ApplicationMessageReceivedAsync += ProcessMessageAsync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
            return;

        _stoppingToken = stoppingToken;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_client.IsConnected)
                    await ConnectAndSubscribeAsync(stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(_options.ReconnectDelaySeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MQTT Edge uplink consumer connection failed.");
                await DelayBeforeReconnectAsync(stoppingToken);
            }
        }
    }

    private async Task ConnectAndSubscribeAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.ConnectTimeoutSeconds));
        await _client.ConnectAsync(_clientOptions, timeout.Token);

        var topics = EdgeUplinkTopic.Subscriptions(_options.TopicPrefix, _options.ConsumerGroup);
        var subscriptions = new MqttClientSubscribeOptionsBuilder();
        foreach (var topic in topics)
        {
            subscriptions.WithTopicFilter(filter => filter
                .WithTopic(topic)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce));
        }

        await _client.SubscribeAsync(subscriptions.Build(), timeout.Token);
        _logger.LogInformation("MQTT Edge uplink consumer subscribed to {TopicCount} typed topics.", topics.Count);
    }

    private async Task ProcessMessageAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        if (_stoppingToken.IsCancellationRequested)
            return;

        await _messageSlots.WaitAsync(_stoppingToken);
        try
        {
            await ProcessMessageCoreAsync(args.ApplicationMessage, _stoppingToken);
        }
        catch (OperationCanceledException) when (_stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled MQTT Edge uplink processing failure for topic {Topic}.", args.ApplicationMessage.Topic);
        }
        finally
        {
            _messageSlots.Release();
        }
    }

    private async Task ProcessMessageCoreAsync(
        MqttApplicationMessage message,
        CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();
        if (!EdgeUplinkTopic.TryParse(
                _options.TopicPrefix,
                message.Topic,
                out var endpointId,
                out var messageType))
        {
            _logger.LogWarning("Rejected MQTT Edge uplink with invalid topic {Topic}.", message.Topic);
            IceBotEdgeMetrics.RecordMqttUplink("invalid_topic", "unknown", Stopwatch.GetElapsedTime(startedAt));
            return;
        }

        if (message.Payload.Length == 0 || message.Payload.Length > _options.MaxPayloadBytes)
        {
            _logger.LogWarning(
                "Rejected MQTT Edge uplink payload for endpoint {EndpointId}; size {PayloadBytes}.",
                endpointId,
                message.Payload.Length);
            IceBotEdgeMetrics.RecordMqttUplink("invalid_size", messageType, Stopwatch.GetElapsedTime(startedAt));
            return;
        }
        var bytes = message.Payload.ToArray();

        EdgeUplinkEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<EdgeUplinkEnvelope>(bytes, _serializerOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Rejected malformed MQTT Edge uplink for endpoint {EndpointId}.", endpointId);
            IceBotEdgeMetrics.RecordMqttUplink("malformed", messageType, Stopwatch.GetElapsedTime(startedAt));
            return;
        }

        if (envelope is null)
            return;

        EdgeUplinkResult result;
        if (message.Retain)
        {
            result = TransportResult(
                endpointId, messageType, envelope.MessageId, 400,
                "Retained MQTT uplink messages are not accepted.");
        }
        else try
        {
            using var scope = _scopeFactory.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IEdgeUplinkMessageDispatcher>();
            result = await dispatcher.DispatchAsync(
                endpointId,
                messageType,
                envelope,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "MQTT Edge uplink {MessageId} failed for endpoint {EndpointId}.",
                envelope.MessageId,
                endpointId);
            result = TransportResult(
                endpointId, messageType, envelope.MessageId, 503,
                "Cloud processing failed; retry the same message.");
        }

        await PublishResultAsync(endpointId, result, cancellationToken);
        IceBotEdgeMetrics.RecordMqttUplink(
            result.Succeeded ? "accepted" : result.Retryable ? "retryable" : "rejected",
            messageType,
            Stopwatch.GetElapsedTime(startedAt));
    }

    private static EdgeUplinkResult TransportResult(
        Guid endpointId,
        string messageType,
        Guid messageId,
        int statusCode,
        string message) => new()
        {
            MessageId = messageId,
            EndpointId = endpointId,
            MessageType = messageType,
            ProcessedAt = DateTimeOffset.UtcNow,
            Succeeded = false,
            StatusCode = statusCode,
            Retryable = statusCode is 408 or 425 or 429 || statusCode >= 500,
            Message = message
        };

    private async Task PublishResultAsync(
        Guid endpointId,
        EdgeUplinkResult result,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(result, _serializerOptions);
        var topic = EdgeUplinkTopic.Result(_options.TopicPrefix, endpointId);
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(false)
            .Build();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.PublishTimeoutSeconds));
        await _client.PublishAsync(message, timeout.Token);
    }

    private async Task DelayBeforeReconnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(_options.ReconnectDelaySeconds), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_client.IsConnected)
            await _client.DisconnectAsync();

        var acquired = 0;
        try
        {
            while (acquired < _options.MaxConcurrentMessages)
            {
                await _messageSlots.WaitAsync(cancellationToken);
                acquired++;
            }
        }
        finally
        {
            if (acquired > 0)
                _messageSlots.Release(acquired);
        }
    }

    public override void Dispose()
    {
        _client.Dispose();
        _messageSlots.Dispose();
        base.Dispose();
    }
}
