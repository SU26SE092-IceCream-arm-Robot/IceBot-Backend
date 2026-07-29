namespace Infrastructure.EdgeIntegration.Mqtt;

public sealed class EdgeCommandMqttOptions
{
    public const string SectionName = "EdgeCommandMqtt";

    public bool Enabled { get; set; }
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public bool UseTls { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string ClientId { get; set; } = "icebot-cloud-backend";
    public string TopicPrefix { get; set; } = "icebot";
    public int ConnectTimeoutSeconds { get; set; } = 5;
    public int PublishTimeoutSeconds { get; set; } = 6;
    public int PublishRetryCount { get; set; } = 1;
    public int PublishRetryDelayMilliseconds { get; set; } = 250;
}
