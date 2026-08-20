using Application.Devices.Connectivity.Results;
using Domain.Devices.Telemetry;
using Application.Devices.Telemetry.Results;
using Domain.Common.Enums;
using Domain.Devices.Catalog;
using Domain.Tenants.Entities;

namespace Application.Devices.Telemetry.Abstractions;

public interface IKioskTelemetryStore
{
    Task<KioskStatusOverviewResult> GetKioskStatusOverviewAsync(
        Guid? organizationId,
        Guid? storeId,
        bool isSystemAdmin,
        IReadOnlyCollection<Guid> allowedOrganizationIds,
        IReadOnlyCollection<Guid> allowedStoreIds,
        IReadOnlyCollection<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default);

    Task<Kiosk?> GetKioskByIdAsync(Guid kioskId, CancellationToken cancellationToken = default);
    Task<Domain.Devices.Connectivity.KioskConnectivityProjection?> GetConnectivityAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default);

    Task<int> CountHeartbeatsAsync(
        Guid kioskId,
        KioskHeartbeatStatus? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default);

    Task<List<KioskHeartbeat>> ListHeartbeatsAsync(
        Guid kioskId,
        KioskHeartbeatStatus? status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountDeviceEventsAsync(
        Guid kioskId,
        SeverityLevel? minSeverity,
        string? eventType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default);

    Task<List<DeviceEvent>> ListDeviceEventsAsync(
        Guid kioskId,
        SeverityLevel? minSeverity,
        string? eventType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
