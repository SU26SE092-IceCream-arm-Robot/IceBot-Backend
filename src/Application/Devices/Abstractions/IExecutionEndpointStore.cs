using Domain.Devices.Entities;

namespace Application.Devices.Abstractions;

public interface IExecutionEndpointStore
{
    Task<KioskExecutionEndpoint?> GetByIdForCredentialRotationAsync(
        Guid endpointId,
        CancellationToken cancellationToken = default);

    Task<bool> CredentialReferenceExistsAsync(
        string credentialReference,
        CancellationToken cancellationToken = default);

    Task AddCredentialBindingAsync(
        ExecutionEndpointCredentialBinding credentialBinding,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
