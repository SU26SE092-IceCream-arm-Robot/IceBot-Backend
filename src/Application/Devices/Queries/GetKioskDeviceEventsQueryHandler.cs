using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Shared.Wrappers;
using Application.Tenants.Kiosks;

namespace Application.Devices.Queries;

public sealed class GetKioskDeviceEventsQueryHandler
{
    private readonly IKioskTelemetryStore _telemetryStore;

    public GetKioskDeviceEventsQueryHandler(IKioskTelemetryStore telemetryStore)
    {
        _telemetryStore = telemetryStore;
    }

    public async Task<PagedResult<DeviceEventResult>> HandleAsync(
        GetKioskDeviceEventsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var kiosk = await _telemetryStore.GetKioskByIdAsync(query.KioskId, cancellationToken);
        if (kiosk is null)
        {
            return PagedResult<DeviceEventResult>.Fail("Kiosk not found.", 404, pageNumber, pageSize);
        }

        if (!KioskAccessRules.CanAccessKiosk(query.UserContext, kiosk))
        {
            return PagedResult<DeviceEventResult>.Forbidden("Access denied.", pageNumber, pageSize);
        }

        var totalCount = await _telemetryStore.CountDeviceEventsAsync(
            query.KioskId,
            query.MinSeverity,
            query.EventType,
            query.From,
            query.To,
            cancellationToken);

        var events = await _telemetryStore.ListDeviceEventsAsync(
            query.KioskId,
            query.MinSeverity,
            query.EventType,
            query.From,
            query.To,
            pageNumber,
            pageSize,
            cancellationToken);

        return PagedResult<DeviceEventResult>.Success(
            events.Select(KioskTelemetryResultMapper.ToResult),
            totalCount,
            pageNumber,
            pageSize,
            "Kiosk device events retrieved successfully.");
    }
}
