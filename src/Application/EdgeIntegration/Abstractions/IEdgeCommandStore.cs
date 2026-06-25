using Domain.Sync.Entities;

namespace Application.EdgeIntegration.Abstractions;

public interface IEdgeCommandStore
{
    Task<EdgeCommand?> GetByIdAsync(Guid commandId, CancellationToken cancellationToken = default);

    Task AddAsync(EdgeCommand command, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
