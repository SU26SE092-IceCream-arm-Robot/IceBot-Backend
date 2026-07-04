using System.Buffers;
using System.Text;
using System.Text.Json;
using Application.Devices.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using Polly;
using Polly.Retry;

namespace Infrastructure.EdgeIntegration.Mqtt;

public sealed class MosquittoDynamicSecurityCredentialProvisioner : IMqttEndpointCredentialProvisioner
{
    private const string ControlTopic = "$CONTROL/dynamic-security/v1";
    private const string ResponseTopic = "$CONTROL/dynamic-security/v1/response";
    private readonly MqttCredentialProvisioningOptions _options;
    private readonly ILogger<MosquittoDynamicSecurityCredentialProvisioner> _logger;
    private readonly ResiliencePipeline _commandPipeline;

    public MosquittoDynamicSecurityCredentialProvisioner(
        IOptions<MqttCredentialProvisioningOptions> options,
        ILogger<MosquittoDynamicSecurityCredentialProvisioner> logger)
    {
        _options = options.Value;
        _logger = logger;
        _commandPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<MqttCredentialTransportException>(),
                MaxRetryAttempts = _options.RetryCount,
                Delay = TimeSpan.FromMilliseconds(_options.RetryDelayMilliseconds),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Retrying Mosquitto dynamic-security command after transport failure. Attempt={AttemptNumber}.",
                        args.AttemptNumber + 1);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public string ProviderName => _options.Provider;

    public string GetSubscribeTopic(Guid endpointId) =>
        $"{_options.TopicPrefix.Trim().Trim('/')}/execution-endpoints/{endpointId:D}/commands/available";

    public async Task ProvisionOrReplaceAsync(
        Guid endpointId,
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        var createError = await ExecuteCommandAsync(new
        {
            command = "createClient",
            username,
            password,
            clientid = endpointId.ToString("D"),
            textname = $"IceBot execution endpoint {endpointId:D}",
            roles = new[] { new { rolename = _options.SubscriberRole, priority = -1 } }
        }, cancellationToken);

        if (createError is null) return;
        if (!createError.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(createError);

        ThrowIfError(await ExecuteCommandAsync(new { command = "setClientPassword", username, password }, cancellationToken));
        ThrowIfError(await ExecuteCommandAsync(new { command = "enableClient", username }, cancellationToken));
        var roleError = await ExecuteCommandAsync(new
        {
            command = "addClientRole",
            username,
            rolename = _options.SubscriberRole,
            priority = -1
        }, cancellationToken);
        if (roleError is not null && !roleError.Contains("already", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(roleError);
    }

    public async Task RevokeAsync(
        Guid endpointId,
        string username,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        ThrowIfError(await ExecuteCommandAsync(new { command = "disableClient", username }, cancellationToken));
    }

    private async Task<string?> ExecuteCommandAsync(object command, CancellationToken cancellationToken)
    {
        string? result = null;
        await _commandPipeline.ExecuteAsync(
            async resilienceToken => result = await SendOnceAsync(command, resilienceToken),
            cancellationToken);
        return result;
    }

    private async Task<string?> SendOnceAsync(object command, CancellationToken cancellationToken)
    {
        var factory = new MqttClientFactory();
        using var client = factory.CreateMqttClient();
        var response = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        client.ApplicationMessageReceivedAsync += args =>
        {
            if (args.ApplicationMessage.Topic == ResponseTopic)
                response.TrySetResult(Encoding.UTF8.GetString(args.ApplicationMessage.Payload.ToArray()));
            return Task.CompletedTask;
        };

        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithClientId($"icebot-credential-admin-{Guid.NewGuid():N}")
            .WithTcpServer(_options.Host, _options.Port)
            .WithCredentials(_options.AdminUsername, _options.AdminPassword);
        if (_options.UseTls) optionsBuilder.WithTlsOptions(tls => tls.UseTls());

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        string responseJson;
        try
        {
            await client.ConnectAsync(optionsBuilder.Build(), timeout.Token);
            await client.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(filter => filter.WithTopic(ResponseTopic).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
                .Build(), timeout.Token);

            var payload = JsonSerializer.Serialize(new { commands = new[] { command } });
            await client.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(ControlTopic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build(), timeout.Token);
            responseJson = await response.Task.WaitAsync(timeout.Token);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MqttCredentialTransportException("Mosquitto dynamic-security command timed out.", ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new MqttCredentialTransportException("Mosquitto dynamic-security transport failed.", ex);
        }

        await client.DisconnectAsync();
        using var document = JsonDocument.Parse(responseJson);
        var first = document.RootElement.GetProperty("responses")[0];
        return first.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String
            ? error.GetString()
            : null;
    }

    private void EnsureEnabled()
    {
        if (!_options.Enabled)
            throw new InvalidOperationException("MQTT credential provisioning is disabled.");
    }

    private static void ThrowIfError(string? error)
    {
        if (!string.IsNullOrWhiteSpace(error)) throw new InvalidOperationException(error);
    }

    private sealed class MqttCredentialTransportException : Exception
    {
        public MqttCredentialTransportException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
