using System.Buffers;
using System.Text;
using System.Text.Json;
using Application.Devices.Abstractions;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;

namespace Infrastructure.EdgeIntegration.Mqtt;

public sealed class MosquittoDynamicSecurityCredentialProvisioner : IMqttEndpointCredentialProvisioner
{
    private const string ControlTopic = "$CONTROL/dynamic-security/v1";
    private const string ResponseTopic = "$CONTROL/dynamic-security/v1/response";
    private readonly MqttCredentialProvisioningOptions _options;

    public MosquittoDynamicSecurityCredentialProvisioner(IOptions<MqttCredentialProvisioningOptions> options)
    {
        _options = options.Value;
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
        var createError = await SendAsync(new
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

        ThrowIfError(await SendAsync(new { command = "setClientPassword", username, password }, cancellationToken));
        ThrowIfError(await SendAsync(new { command = "enableClient", username }, cancellationToken));
        var roleError = await SendAsync(new
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
        ThrowIfError(await SendAsync(new { command = "disableClient", username }, cancellationToken));
    }

    private async Task<string?> SendAsync(object command, CancellationToken cancellationToken)
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

        var responseJson = await response.Task.WaitAsync(timeout.Token);
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
}
