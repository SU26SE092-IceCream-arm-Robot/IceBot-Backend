using Domain.Devices.Telemetry;
using Application.Devices.Catalog.Abstractions;
using Application.Devices.ExecutionEndpoints.Abstractions;
using Application.Devices.Telemetry.Abstractions;
using Application.Devices.Connectivity.Abstractions;
using Application.Devices.Credentials.Abstractions;
using Application.Devices.Catalog.Results;
using Application.Devices.ExecutionEndpoints.Results;
using Application.Devices.Telemetry.Results;
using Application.Devices.Connectivity.Results;
using Application.Devices.Credentials.Results;
using Domain.Common.Enums;
using Domain.Devices.Catalog;
using Domain.Tenants.Entities;
using Domain.Devices.Connectivity;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Devices.Telemetry.Persistence;

public sealed class KioskTelemetryStore : IKioskTelemetryStore
{
    private readonly IceBotDbContext _dbContext;

    public KioskTelemetryStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<KioskStatusOverviewResult> GetKioskStatusOverviewAsync(
        Guid? organizationId,
        Guid? storeId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Kiosks.WhereNotDeleted().AsNoTracking();

        if (organizationId.HasValue)
        {
            query = query.Where(k => k.OrganizationId == organizationId.Value);
        }

        if (storeId.HasValue)
        {
            query = query.Where(k => k.StoreId == storeId.Value);
        }

        if (!isSystemAdmin)
        {
            var allowedOrgs = allowedOrganizationIds ?? Array.Empty<Guid>();
            var allowedStores = allowedStoreIds ?? Array.Empty<Guid>();
            var allowedKiosks = allowedKioskIds ?? Array.Empty<Guid>();

            query = query.Where(k =>
                allowedOrgs.Contains(k.OrganizationId) ||
                allowedStores.Contains(k.StoreId) ||
                allowedKiosks.Contains(k.Id));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var statusCounts = await query
            .GroupBy(k => k.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var byLifecycleStatus = statusCounts
            .Select(sc => new KioskStatusSummaryDto
            {
                Status = sc.Status.ToString(),
                Count = sc.Count
            })
            .ToList();

        var kiosksList = await query
            .Include(k => k.Store)
            .ToListAsync(cancellationToken);

        var kioskIds = kiosksList.Select(k => k.Id).ToList();

        var connectivityByKiosk = await _dbContext.KioskConnectivityProjections.AsNoTracking()
            .Where(connectivity => kioskIds.Contains(connectivity.KioskId))
            .ToDictionaryAsync(connectivity => connectivity.KioskId, cancellationToken);

        var byConnectivityStatus = connectivityByKiosk.Values
            .GroupBy(connectivity => connectivity.Status)
            .Select(group => new KioskStatusSummaryDto
            {
                Status = group.Key.ToString(),
                Count = group.Count()
            })
            .ToList();
        var unknownConnectivityCount = totalCount - connectivityByKiosk.Count;
        if (unknownConnectivityCount > 0)
        {
            byConnectivityStatus.Add(new KioskStatusSummaryDto
            {
                Status = KioskConnectivityStatus.Unknown.ToString(),
                Count = unknownConnectivityCount
            });
        }

        var lastHeartbeats = await _dbContext.KioskHeartbeats.AsNoTracking()
            .Where(hb => kioskIds.Contains(hb.KioskId))
            .GroupBy(hb => hb.KioskId)
            .Select(g => new { KioskId = g.Key, LastReportedAt = g.Max(hb => hb.ReportedAt) })
            .ToDictionaryAsync(x => x.KioskId, x => x.LastReportedAt, cancellationToken);

        var lastEvents = await _dbContext.DeviceEvents.AsNoTracking()
            .Where(de => de.KioskId.HasValue && kioskIds.Contains(de.KioskId.Value))
            .GroupBy(de => de.KioskId!.Value)
            .Select(g => new
            {
                KioskId = g.Key,
                LastEvent = g.OrderByDescending(de => de.OccurredAt).Select(de => new { de.OccurredAt, de.Severity }).FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.KioskId, x => x.LastEvent, cancellationToken);

        var readiness = await _dbContext.ExecutionEndpointReadinessProjections.AsNoTracking()
            .Where(x => kioskIds.Contains(x.KioskId))
            .Select(x => new
            {
                x.KioskId,
                x.Readiness,
                x.Activity,
                x.Safety,
                x.FaultCode,
                x.ExecutorReportedAt,
                Available = x.Capabilities.Count(c => c.IsAvailable),
                Unavailable = x.Capabilities.Count(c => !c.IsAvailable)
            })
            .ToListAsync(cancellationToken);
        var readinessByKiosk = readiness.GroupBy(x => x.KioskId).ToDictionary(
            g => g.Key,
            g => g.OrderByDescending(x => x.ExecutorReportedAt).First());

        var items = kiosksList.Select(k =>
        {
            DateTimeOffset? lastHeartbeat = lastHeartbeats.TryGetValue(k.Id, out var hbAt) ? hbAt : k.LastOnlineAt;

            string? severity = null;
            DateTimeOffset? lastEventAt = null;
            if (lastEvents.TryGetValue(k.Id, out var lastEv) && lastEv != null)
            {
                severity = lastEv.Severity.ToString();
                lastEventAt = lastEv.OccurredAt;
            }

            readinessByKiosk.TryGetValue(k.Id, out var ready);
            return new KioskStatusOverviewItemDto
            {
                KioskId = k.Id,
                KioskCode = k.Code,
                KioskName = k.Name,
                OrganizationId = k.OrganizationId,
                StoreId = k.StoreId,
                StoreName = k.Store?.Name ?? string.Empty,
                LifecycleStatus = k.Status.ToString(),
                ConnectivityStatus = connectivityByKiosk.TryGetValue(k.Id, out var connectivity)
                    ? connectivity.Status.ToString()
                    : KioskConnectivityStatus.Unknown.ToString(),
                LastHeartbeatAt = lastHeartbeat,
                LastEventSeverity = severity,
                LastEventAt = lastEventAt,
                ExecutionReadiness = ready?.Readiness.ToString(),
                ExecutionActivity = ready?.Activity.ToString(),
                ExecutionSafety = ready?.Safety.ToString(),
                ExecutionFaultCode = ready?.FaultCode,
                ReadinessReportedAt = ready?.ExecutorReportedAt,
                AvailableCapabilityCount = ready?.Available ?? 0,
                UnavailableCapabilityCount = ready?.Unavailable ?? 0
            };
        }).ToList();

        return new KioskStatusOverviewResult
        {
            TotalCount = totalCount,
            ByLifecycleStatus = byLifecycleStatus,
            ByConnectivityStatus = byConnectivityStatus,
            Items = items
        };
    }

    public Task<Kiosk?> GetKioskByIdAsync(Guid kioskId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Kiosks.WhereNotDeleted()
            .AsNoTracking()
            .FirstOrDefaultAsync(kiosk => kiosk.Id == kioskId, cancellationToken);
    }

    public Task<KioskConnectivityProjection?> GetConnectivityAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default) =>
        _dbContext.KioskConnectivityProjections.AsNoTracking()
            .FirstOrDefaultAsync(connectivity => connectivity.KioskId == kioskId, cancellationToken);

    public Task<int> CountHeartbeatsAsync(
        Guid kioskId,
        KioskHeartbeatStatus? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        return ApplyHeartbeatFilters(kioskId, status, from, to)
            .CountAsync(cancellationToken);
    }

    public Task<List<KioskHeartbeat>> ListHeartbeatsAsync(
        Guid kioskId,
        KioskHeartbeatStatus? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return ApplyHeartbeatFilters(kioskId, status, from, to)
            .OrderByDescending(heartbeat => heartbeat.ReportedAt)
            .ThenByDescending(heartbeat => heartbeat.ReceivedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountDeviceEventsAsync(
        Guid kioskId,
        SeverityLevel? minSeverity,
        string? eventType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        return ApplyDeviceEventFilters(kioskId, minSeverity, eventType, from, to)
            .CountAsync(cancellationToken);
    }

    public Task<List<DeviceEvent>> ListDeviceEventsAsync(
        Guid kioskId,
        SeverityLevel? minSeverity,
        string? eventType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return ApplyDeviceEventFilters(kioskId, minSeverity, eventType, from, to)
            .OrderByDescending(deviceEvent => deviceEvent.OccurredAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<KioskHeartbeat> ApplyHeartbeatFilters(
        Guid kioskId,
        KioskHeartbeatStatus? status,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        var query = _dbContext.KioskHeartbeats
            .AsNoTracking()
            .Where(heartbeat => heartbeat.KioskId == kioskId);

        if (status.HasValue)
        {
            query = query.Where(heartbeat => heartbeat.Status == status.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(heartbeat => heartbeat.ReportedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(heartbeat => heartbeat.ReportedAt <= to.Value);
        }

        return query;
    }

    private IQueryable<DeviceEvent> ApplyDeviceEventFilters(
        Guid kioskId,
        SeverityLevel? minSeverity,
        string? eventType,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        var query = _dbContext.DeviceEvents
            .AsNoTracking()
            .Where(deviceEvent => deviceEvent.KioskId == kioskId);

        if (minSeverity.HasValue)
        {
            query = query.Where(deviceEvent => deviceEvent.Severity >= minSeverity.Value);
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            var normalizedEventType = eventType.Trim();
            query = query.Where(deviceEvent => deviceEvent.EventType == normalizedEventType);
        }

        if (from.HasValue)
        {
            query = query.Where(deviceEvent => deviceEvent.OccurredAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(deviceEvent => deviceEvent.OccurredAt <= to.Value);
        }

        return query;
    }
}
