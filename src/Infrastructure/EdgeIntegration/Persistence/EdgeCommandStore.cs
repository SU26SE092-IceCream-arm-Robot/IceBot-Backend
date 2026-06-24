using Application.EdgeIntegration.Abstractions;
using Domain.Sync.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.EdgeIntegration.Persistence;

public sealed class EdgeCommandStore : IEdgeCommandStore
{
    private readonly IceBotDbContext _dbContext;

    public EdgeCommandStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<EdgeCommand?> GetByIdAsync(Guid commandId, CancellationToken cancellationToken = default)
    {
        return _dbContext.EdgeCommands
            .Include(command => command.DeliveryAttempts)
            .FirstOrDefaultAsync(command => command.Id == commandId, cancellationToken);
    }

    public Task AddAsync(EdgeCommand command, CancellationToken cancellationToken = default)
    {
        return _dbContext.EdgeCommands.AddAsync(command, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
