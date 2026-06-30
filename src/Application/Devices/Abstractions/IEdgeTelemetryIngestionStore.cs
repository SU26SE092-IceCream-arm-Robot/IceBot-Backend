using Domain.Devices.Entities;
using Domain.Tenants.Entities;
using Domain.Operations.Entities;

namespace Application.Devices.Abstractions;

public interface IEdgeTelemetryIngestionStore
{
    Task<T> ExecuteHeartbeatIngestionAsync<T>(
        Guid kioskId,
        Guid originNodeId,
        long heartbeatSequence,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);

    Task<T> ExecuteDeviceEventIngestionAsync<T>(
        Guid eventId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);

    Task<T> ExecuteConnectivityReconciliationAsync<T>(
        Guid kioskId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);

    Task<List<Guid>> ListConnectivityTimeoutCandidateIdsAsync(
        DateTimeOffset cutoff,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<KioskExecutionEndpoint?> GetEndpointAsync(
        Guid endpointId,
        CancellationToken cancellationToken = default);

    Task<KioskHeartbeat?> GetHeartbeatAsync(
        Guid kioskId,
        Guid originNodeId,
        long heartbeatSequence,
        CancellationToken cancellationToken = default);

    Task<DeviceEvent?> GetDeviceEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<Kiosk?> GetKioskAsync(Guid kioskId, CancellationToken cancellationToken = default);

    Task<Device?> GetDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default);

    Task AddHeartbeatAsync(KioskHeartbeat heartbeat, CancellationToken cancellationToken = default);

    Task AddDeviceEventAsync(DeviceEvent deviceEvent, CancellationToken cancellationToken = default);

    Task AddAlertAsync(Alert alert, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
