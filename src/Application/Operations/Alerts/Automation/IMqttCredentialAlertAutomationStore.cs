using Domain.Devices.ExecutionEndpoints;
using Domain.Operations.Entities;

namespace Application.Operations.Alerts.Automation;

public interface IMqttCredentialAlertAutomationStore
{
    Task<IReadOnlyList<Guid>> ListFailureStateEndpointIdsAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListActiveAlertEndpointIdsAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<KioskExecutionEndpoint?> GetEndpointAsync(
        Guid endpointId,
        CancellationToken cancellationToken = default);

    Task<List<Alert>> ListActiveAlertsAsync(
        Guid endpointId,
        CancellationToken cancellationToken = default);

    Task AddAlertAsync(Alert alert, CancellationToken cancellationToken = default);
    Task AcquireLockAsync(Guid endpointId, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);
}
