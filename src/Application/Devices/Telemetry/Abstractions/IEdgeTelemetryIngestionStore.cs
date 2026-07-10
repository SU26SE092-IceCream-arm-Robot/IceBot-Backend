using Domain.Devices.Telemetry;
using Domain.Devices.ExecutionEndpoints;
using Domain.Devices.Catalog;
using Domain.Tenants.Entities;
using Domain.Operations.Entities;

namespace Application.Devices.Telemetry.Abstractions;

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
        Guid kioskId,
        Guid deviceId,
        string alertCorrelationKey,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);

    Task<T> ExecuteConnectivityReconciliationAsync<T>(
        Guid kioskId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);

    Task<T> ExecuteOperationLogIngestionAsync<T>(
        Guid sourceEventId,
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

    Task<KioskHeartbeat?> GetLatestHeartbeatAsync(
        Guid kioskId,
        Guid originNodeId,
        CancellationToken cancellationToken = default);

    Task<DeviceEvent?> GetDeviceEventAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<OperationLog?> GetOperationLogAsync(
        Guid sourceEventId,
        CancellationToken cancellationToken = default);

    Task<Kiosk?> GetKioskAsync(Guid kioskId, CancellationToken cancellationToken = default);

    Task<Device?> GetDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default);

    Task<bool> OrderBelongsToKioskAsync(
        Guid orderId,
        Guid kioskId,
        CancellationToken cancellationToken = default);

    Task AddHeartbeatAsync(KioskHeartbeat heartbeat, CancellationToken cancellationToken = default);

    Task AddDeviceEventAsync(DeviceEvent deviceEvent, CancellationToken cancellationToken = default);

    Task AddOperationLogAsync(OperationLog operationLog, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
