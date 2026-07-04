namespace Infrastructure.EdgeIntegration.Mqtt;

public interface IMqttEdgeCommandTransport : IAsyncDisposable
{
    Task PublishAsync(string topic, string payload, CancellationToken cancellationToken = default);
}
