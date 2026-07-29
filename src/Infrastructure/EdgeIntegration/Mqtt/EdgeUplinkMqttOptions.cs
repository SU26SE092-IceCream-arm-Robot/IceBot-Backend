namespace Infrastructure.EdgeIntegration.Mqtt;

public sealed class EdgeUplinkMqttOptions
{
    public const string SectionName = "EdgeUplinkMqtt";

    public bool Enabled { get; set; }
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public bool UseTls { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string ClientId { get; set; } = $"icebot-cloud-uplink-{Environment.MachineName}";
    public string TopicPrefix { get; set; } = "icebot";
    public string ConsumerGroup { get; set; } = "icebot-cloud-uplink";
    public int ConnectTimeoutSeconds { get; set; } = 5;
    public int PublishTimeoutSeconds { get; set; } = 6;
    public int ReconnectDelaySeconds { get; set; } = 5;
    public int MaxPayloadBytes { get; set; } = 256 * 1024;
    public int MaxConcurrentMessages { get; set; } = 16;

    public bool HasValidTopicConfiguration() =>
        !string.IsNullOrWhiteSpace(TopicPrefix) &&
        TopicPrefix.IndexOfAny(['+', '#']) < 0 &&
        !string.IsNullOrWhiteSpace(ConsumerGroup) &&
        ConsumerGroup.IndexOfAny(['/', '+', '#']) < 0;
}
