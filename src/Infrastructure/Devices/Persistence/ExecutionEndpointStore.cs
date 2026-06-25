using Application.Devices.Abstractions;
using Domain.Devices.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Devices.Persistence;

public sealed class ExecutionEndpointStore : IExecutionEndpointStore
{
    private readonly IceBotDbContext _dbContext;

    public ExecutionEndpointStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<KioskExecutionEndpoint?> GetByIdForCredentialRotationAsync(
        Guid endpointId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskExecutionEndpoints
            .Include(endpoint => endpoint.Kiosk)
            .Include(endpoint => endpoint.CredentialBinding)
            .FirstOrDefaultAsync(endpoint => endpoint.Id == endpointId, cancellationToken);
    }

    public Task<bool> CredentialReferenceExistsAsync(
        string credentialReference,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ExecutionEndpointCredentialBindings.AnyAsync(
            credential => credential.CredentialReference == credentialReference,
            cancellationToken);
    }

    public Task AddCredentialBindingAsync(
        ExecutionEndpointCredentialBinding credentialBinding,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ExecutionEndpointCredentialBindings.AddAsync(credentialBinding, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
