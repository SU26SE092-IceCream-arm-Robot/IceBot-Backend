namespace Infrastructure.EdgeIntegration.Mqtt;

public sealed class MqttCredentialProvisioningOptions
{
    public const string SectionName = "MqttCredentialProvisioning";
    public bool Enabled { get; set; }
    public string Provider { get; set; } = "MosquittoDynamicSecurity";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public bool UseTls { get; set; }
    public string AdminUsername { get; set; } = "icebot-dynsec-admin";
    public string? AdminPassword { get; set; }
    public string SubscriberRole { get; set; } = "icebot-endpoint-subscriber";
    public string TopicPrefix { get; set; } = "icebot";
    public int TimeoutSeconds { get; set; } = 10;
    public int RetryCount { get; set; } = 1;
    public int RetryDelayMilliseconds { get; set; } = 500;
    public int ReconciliationIntervalSeconds { get; set; } = 60;
    public int ReconciliationBatchSize { get; set; } = 100;
}
