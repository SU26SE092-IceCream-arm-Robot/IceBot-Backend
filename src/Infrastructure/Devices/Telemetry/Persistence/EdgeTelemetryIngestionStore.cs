using Domain.Devices.Telemetry;
using Domain.Devices.ExecutionEndpoints;
using Application.Devices.Catalog.Abstractions;
using Application.Devices.ExecutionEndpoints.Abstractions;
using Application.Devices.Telemetry.Abstractions;
using Application.Devices.Credentials.Abstractions;
using Domain.Devices.Catalog;
using Domain.Tenants.Entities;
using Domain.Operations.Entities;
using Domain.Devices.Connectivity;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Devices.Telemetry.Persistence;

public sealed class EdgeTelemetryIngestionStore : IEdgeTelemetryIngestionStore, IAlertIngestionStore
{
    private readonly IceBotDbContext _dbContext;

    public EdgeTelemetryIngestionStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<T> ExecuteHeartbeatIngestionAsync<T>(
        Guid kioskId,
        Guid originNodeId,
        long heartbeatSequence,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        return ExecuteSerializedAsync(
            [$"kiosk-connectivity:{kioskId:D}", $"heartbeat:{kioskId:D}:{originNodeId:D}:{heartbeatSequence}"],
            action,
            cancellationToken);
    }

    public Task<T> ExecuteDeviceEventIngestionAsync<T>(
        Guid eventId,
        Guid kioskId,
        Guid deviceId,
        string alertCorrelationKey,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        return ExecuteSerializedAsync(
            [$"device-event:{eventId:D}", $"alert-correlation:{kioskId:D}:{deviceId:D}:{alertCorrelationKey}"],
            action,
            cancellationToken);
    }

    public Task<T> ExecuteConnectivityReconciliationAsync<T>(
        Guid kioskId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default) =>
        ExecuteSerializedAsync([$"kiosk-connectivity:{kioskId:D}"], action, cancellationToken);

    public Task<T> ExecuteOperationLogIngestionAsync<T>(
        Guid sourceEventId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default) =>
        ExecuteSerializedAsync([$"operation-log:{sourceEventId:D}"], action, cancellationToken);

    public Task<List<Guid>> ListConnectivityTimeoutCandidateIdsAsync(
        DateTimeOffset cutoff,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskConnectivityProjections.AsNoTracking()
            .Where(connectivity =>
                connectivity.Status != KioskConnectivityStatus.Unreachable &&
                connectivity.LastObservedAt < cutoff &&
                _dbContext.Kiosks.WhereNotDeleted().Any(kiosk =>
                    kiosk.Id == connectivity.KioskId && kiosk.Status == Domain.Tenants.Enums.KioskStatus.Active))
            .OrderBy(connectivity => connectivity.LastObservedAt)
            .Select(connectivity => connectivity.KioskId)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    private async Task<T> ExecuteSerializedAsync<T>(
        IReadOnlyCollection<string> lockKeys,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        foreach (var lockKey in lockKeys)
        {
            await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0));",
                cancellationToken);
        }
        var result = await action(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public Task<KioskExecutionEndpoint?> GetEndpointAsync(Guid endpointId, CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskExecutionEndpoints.WhereNotDeleted().AsNoTracking()
            .Include(endpoint => endpoint.CredentialBinding)
            .FirstOrDefaultAsync(endpoint => endpoint.Id == endpointId, cancellationToken);
    }

    public Task<KioskHeartbeat?> GetHeartbeatAsync(
        Guid kioskId,
        Guid originNodeId,
        long heartbeatSequence,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskHeartbeats.AsNoTracking().FirstOrDefaultAsync(
            heartbeat => heartbeat.KioskId == kioskId && heartbeat.NodeId == originNodeId &&
                         heartbeat.HeartbeatSequence == heartbeatSequence,
            cancellationToken);
    }

    public Task<KioskHeartbeat?> GetLatestHeartbeatAsync(
        Guid kioskId,
        Guid originNodeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskHeartbeats.AsNoTracking()
            .Where(heartbeat => heartbeat.KioskId == kioskId && heartbeat.NodeId == originNodeId)
            .OrderByDescending(heartbeat => heartbeat.HeartbeatSequence)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<DeviceEvent?> GetDeviceEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return _dbContext.DeviceEvents.AsNoTracking()
            .FirstOrDefaultAsync(deviceEvent => deviceEvent.EventId == eventId, cancellationToken);
    }

    public Task<OperationLog?> GetOperationLogAsync(
        Guid sourceEventId,
        CancellationToken cancellationToken = default) =>
        _dbContext.OperationLogs.AsNoTracking()
            .FirstOrDefaultAsync(log => log.SourceEventId == sourceEventId, cancellationToken);

    public Task<Kiosk?> GetKioskAsync(Guid kioskId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Kiosks.WhereNotDeleted()
            .Include(kiosk => kiosk.Organization)
            .Include(kiosk => kiosk.Store)
            .FirstOrDefaultAsync(kiosk => kiosk.Id == kioskId, cancellationToken);
    }

    public Task<KioskConnectivityProjection?> GetConnectivityAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default) =>
        _dbContext.KioskConnectivityProjections.FirstOrDefaultAsync(
            connectivity => connectivity.KioskId == kioskId, cancellationToken);

    public Task<Device?> GetDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Devices.WhereNotDeleted().Include(device => device.Kiosk)
            .FirstOrDefaultAsync(device => device.Id == deviceId, cancellationToken);
    }

    public Task<bool> OrderBelongsToKioskAsync(
        Guid orderId,
        Guid kioskId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Orders.WhereNotDeleted().AnyAsync(order => order.Id == orderId && order.KioskId == kioskId, cancellationToken);

    public Task AddHeartbeatAsync(KioskHeartbeat heartbeat, CancellationToken cancellationToken = default) =>
        _dbContext.KioskHeartbeats.AddAsync(heartbeat, cancellationToken).AsTask();

    public Task AddConnectivityAsync(KioskConnectivityProjection connectivity, CancellationToken cancellationToken = default) =>
        _dbContext.KioskConnectivityProjections.AddAsync(connectivity, cancellationToken).AsTask();

    public Task AddDeviceEventAsync(DeviceEvent deviceEvent, CancellationToken cancellationToken = default) =>
        _dbContext.DeviceEvents.AddAsync(deviceEvent, cancellationToken).AsTask();

    public Task AddAlertAsync(Alert alert, CancellationToken cancellationToken = default) =>
        _dbContext.Alerts.AddAsync(alert, cancellationToken).AsTask();

    public Task<Alert?> FindCorrelatableAlertAsync(
        Guid kioskId,
        Guid deviceId,
        string correlationKey,
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        CancellationToken cancellationToken = default) =>
        _dbContext.Alerts
            .Where(alert =>
                alert.DeletedAt == null &&
                alert.KioskId == kioskId &&
                alert.DeviceId == deviceId &&
                alert.CorrelationKey == correlationKey &&
                alert.Status != Domain.Operations.Enums.AlertStatus.Resolved &&
                alert.Status != Domain.Operations.Enums.AlertStatus.Suppressed &&
                alert.LastOccurredAt >= windowStart &&
                alert.LastOccurredAt <= windowEnd)
            .OrderByDescending(alert => alert.LastOccurredAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task AddOperationLogAsync(OperationLog operationLog, CancellationToken cancellationToken = default) =>
        _dbContext.OperationLogs.AddAsync(operationLog, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
