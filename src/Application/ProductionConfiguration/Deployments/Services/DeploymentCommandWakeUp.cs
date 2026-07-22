using Application.EdgeIntegration.Abstractions;
using Application.Shared.Wrappers;
using Domain.Sync.Entities;
using Domain.Sync.Enums;

namespace Application.ProductionConfiguration.Deployments.Services;

public static class DeploymentCommandWakeUp
{
    public static Task TryPublishAsync<T>(
        ApiResult<T> result,
        Func<T, Guid?> commandId,
        Func<T, Guid> endpointId,
        IEdgeCommandWakeUpPublisher publisher,
        CancellationToken cancellationToken) where T : class =>
        result.Succeeded && result.Data is { } data && commandId(data) is Guid edgeCommandId
            ? publisher.TryPublishAsync(
                new EdgeCommandWakeUp(edgeCommandId, endpointId(data), EdgeCommandType.DeployConfiguration,
                    DateTimeOffset.UtcNow), cancellationToken)
            : Task.CompletedTask;
}
