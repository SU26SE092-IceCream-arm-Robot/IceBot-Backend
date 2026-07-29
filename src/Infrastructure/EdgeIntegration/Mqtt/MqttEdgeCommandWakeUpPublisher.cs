using System.Text.Json;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.Observability;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Infrastructure.EdgeIntegration.Mqtt;

public sealed class MqttEdgeCommandWakeUpPublisher : IEdgeCommandWakeUpPublisher
{
    private readonly EdgeCommandMqttOptions _options;
    private readonly ILogger<MqttEdgeCommandWakeUpPublisher> _logger;
    private readonly IMqttEdgeCommandTransport _transport;
    private readonly ResiliencePipeline _publishPipeline;

    public MqttEdgeCommandWakeUpPublisher(
        IOptions<EdgeCommandMqttOptions> options,
        IMqttEdgeCommandTransport transport,
        ILogger<MqttEdgeCommandWakeUpPublisher> logger)
    {
        _options = options.Value;
        _transport = transport;
        _logger = logger;
        _publishPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(exception =>
                    exception is not OperationCanceledException),
                MaxRetryAttempts = _options.PublishRetryCount,
                Delay = TimeSpan.FromMilliseconds(_options.PublishRetryDelayMilliseconds),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Retrying MQTT command wake-up publish after transient failure. Attempt={AttemptNumber}.",
                        args.AttemptNumber + 1);
                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(_options.PublishTimeoutSeconds))
            .Build();
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
            await _publishPipeline.ExecuteAsync(
                async resilienceToken => await PublishOnceAsync(notification, resilienceToken),
                cancellationToken);
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

    private async ValueTask PublishOnceAsync(
        EdgeCommandWakeUp notification,
        CancellationToken cancellationToken)
    {
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
        await _transport.PublishAsync(topic, payload, cancellationToken);
    }
}
