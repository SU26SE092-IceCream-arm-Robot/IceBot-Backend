using Application.EdgeIntegration.Abstractions;
using Domain.Devices.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;

namespace Infrastructure.EdgeIntegration.Persistence;

public sealed class ExecutionEndpointTransportAuthStore : IExecutionEndpointTransportAuthStore
{
    private readonly IceBotDbContext _dbContext;
    public ExecutionEndpointTransportAuthStore(IceBotDbContext dbContext) => _dbContext = dbContext;

    public Task<KioskExecutionEndpoint?> GetEndpointAsync(Guid endpointId, CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskExecutionEndpoints.AsNoTracking()
            .Include(endpoint => endpoint.CredentialBinding)
            .FirstOrDefaultAsync(endpoint => endpoint.Id == endpointId, cancellationToken);
    }

    public async Task<bool> TryRegisterNonceAsync(
        ExecutionEndpointRequestNonce nonce,
        CancellationToken cancellationToken = default)
    {
        EntityEntry<ExecutionEndpointRequestNonce> entry = await _dbContext.ExecutionEndpointRequestNonces.AddAsync(nonce, cancellationToken);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            entry.State = EntityState.Detached;
            return false;
        }
    }
}
