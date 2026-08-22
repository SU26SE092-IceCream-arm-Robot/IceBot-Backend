using Domain.Devices.Catalog;
using Domain.Devices.ExecutionEndpoints;
using Domain.Devices.ExecutionEndpoints.Projections;

namespace Application.Devices.Connectivity.Abstractions;

public interface IExecutionReadinessStore
{
    Task<T> ExecuteSerializedAsync<T>(Guid endpointId, Func<CancellationToken, Task<T>> action, CancellationToken ct = default);
    Task<KioskExecutionEndpoint?> GetEndpointAsync(Guid endpointId, CancellationToken ct = default);
    Task<ExecutionEndpointReadinessProjection?> GetProjectionAsync(Guid endpointId, bool tracked, CancellationToken ct = default);
    Task AddProjectionAsync(ExecutionEndpointReadinessProjection projection, CancellationToken ct = default);
    void ReplaceCapabilities(ExecutionEndpointReadinessProjection projection, IReadOnlyCollection<ExecutionEndpointCapabilityProjection> capabilities);
    Task SaveChangesAsync(CancellationToken ct = default);
}
