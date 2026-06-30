using Domain.Sync.Enums;

namespace Application.EdgeIntegration.Abstractions;

public interface IEdgeCommandWakeUpPublisher
{
    Task<bool> TryPublishAsync(
        EdgeCommandWakeUp notification,
        CancellationToken cancellationToken = default);
}

public sealed record EdgeCommandWakeUp(
    Guid CommandId,
    Guid TargetExecutionEndpointId,
    EdgeCommandType CommandType,
    DateTimeOffset NotifiedAt);
