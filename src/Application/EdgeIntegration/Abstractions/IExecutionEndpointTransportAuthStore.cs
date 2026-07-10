using Domain.Devices.ExecutionEndpoints;
using Domain.Devices.Catalog;

namespace Application.EdgeIntegration.Abstractions;

public interface IExecutionEndpointTransportAuthStore
{
    Task<KioskExecutionEndpoint?> GetEndpointAsync(Guid endpointId, CancellationToken cancellationToken = default);

    Task<bool> TryRegisterNonceAsync(
        ExecutionEndpointRequestNonce nonce,
        CancellationToken cancellationToken = default);
}
