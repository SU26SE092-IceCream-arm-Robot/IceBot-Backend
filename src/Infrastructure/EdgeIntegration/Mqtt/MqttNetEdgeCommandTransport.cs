using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;

namespace Infrastructure.EdgeIntegration.Mqtt;

public sealed class MqttNetEdgeCommandTransport : IMqttEdgeCommandTransport
{
    private readonly EdgeCommandMqttOptions _options;
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _clientOptions;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    public MqttNetEdgeCommandTransport(IOptions<EdgeCommandMqttOptions> options)
    {
        _options = options.Value;
        _client = new MqttClientFactory().CreateMqttClient();

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

    public async Task PublishAsync(string topic, string payload, CancellationToken cancellationToken = default)
    {
        await EnsureConnectedAsync(cancellationToken);
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(false)
            .Build();
        await _client.PublishAsync(message, cancellationToken);
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
            try
            {
                await _client.ConnectAsync(_clientOptions, timeout.Token);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("MQTT broker connection timed out.", ex);
            }
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
