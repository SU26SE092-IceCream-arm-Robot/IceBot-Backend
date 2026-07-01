using System.Text.Json;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Observability;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;

namespace Infrastructure.EdgeIntegration.Mqtt;

public sealed class MqttEdgeCommandWakeUpPublisher : IEdgeCommandWakeUpPublisher, IAsyncDisposable
{
    private readonly EdgeCommandMqttOptions _options;
    private readonly ILogger<MqttEdgeCommandWakeUpPublisher> _logger;
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _clientOptions;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    public MqttEdgeCommandWakeUpPublisher(
        IOptions<EdgeCommandMqttOptions> options,
        ILogger<MqttEdgeCommandWakeUpPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;

        var factory = new MqttClientFactory();
        _client = factory.CreateMqttClient();
        var builder = new MqttClientOptionsBuilder()
            .WithClientId(_options.ClientId)
            .WithTcpServer(_options.Host, _options.Port);

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            builder.WithCredentials(_options.Username, _options.Password);
        }

        if (_options.UseTls)
        {
            builder.WithTlsOptions(tls => tls.UseTls());
        }

        _clientOptions = builder.Build();
    }

    public async Task<bool> TryPublishAsync(
        EdgeCommandWakeUp notification,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            IceBotEdgeMetrics.RecordMqttWakeUp("disabled", notification.CommandType.ToString());
            return false;
        }

        try
        {
            await EnsureConnectedAsync(cancellationToken);
            var topicPrefix = _options.TopicPrefix.Trim().Trim('/');
            var topic = $"{topicPrefix}/execution-endpoints/{notification.TargetExecutionEndpointId:D}/commands/available";
            var payload = JsonSerializer.Serialize(new
            {
                Type = "CommandAvailable",
                notification.CommandId,
                CommandType = notification.CommandType.ToString(),
                notification.TargetExecutionEndpointId,
                notification.NotifiedAt,
                Version = 1
            });
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(false)
                .Build();

            await _client.PublishAsync(message, cancellationToken);
            IceBotEdgeMetrics.RecordMqttWakeUp("succeeded", notification.CommandType.ToString());
            return true;
        }
        catch (Exception ex)
        {
            IceBotEdgeMetrics.RecordMqttWakeUp("failed", notification.CommandType.ToString());
            _logger.LogWarning(
                ex,
                "MQTT command wake-up publish failed for command {CommandId} and endpoint {EndpointId}; periodic pull remains authoritative.",
                notification.CommandId,
                notification.TargetExecutionEndpointId);
            return false;
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_client.IsConnected)
        {
            return;
        }

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_client.IsConnected)
            {
                return;
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(_options.ConnectTimeoutSeconds));
            await _client.ConnectAsync(_clientOptions, timeout.Token);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_client.IsConnected)
        {
            await _client.DisconnectAsync();
        }

        _client.Dispose();
        _connectionLock.Dispose();
    }
}
