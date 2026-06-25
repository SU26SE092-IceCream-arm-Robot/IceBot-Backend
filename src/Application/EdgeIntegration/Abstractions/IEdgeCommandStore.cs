using Domain.Sync.Entities;

namespace Application.EdgeIntegration.Abstractions;

public interface IEdgeCommandStore
{
    Task<Domain.Devices.Entities.KioskExecutionEndpoint?> GetEndpointForCommandAuthAsync(
        Guid endpointId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EdgeCommand>> ListDispatchableAsync(
        Guid kioskId,
        Guid endpointId,
        int maxCommands,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken = default);

    Task<EdgeCommand?> GetByIdAsync(Guid commandId, CancellationToken cancellationToken = default);

    Task AddAsync(EdgeCommand command, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
