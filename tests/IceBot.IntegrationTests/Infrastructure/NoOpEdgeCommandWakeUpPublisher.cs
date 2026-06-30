using Application.EdgeIntegration.Abstractions;

namespace IceBot.IntegrationTests.Infrastructure;

internal sealed class NoOpEdgeCommandWakeUpPublisher : IEdgeCommandWakeUpPublisher
{
    public List<EdgeCommandWakeUp> Notifications { get; } = [];
    public bool PublishResult { get; init; } = true;

    public Task<bool> TryPublishAsync(
        EdgeCommandWakeUp notification,
        CancellationToken cancellationToken = default)
    {
        Notifications.Add(notification);
        return Task.FromResult(PublishResult);
    }
}
