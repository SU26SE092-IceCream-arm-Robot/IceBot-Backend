using Application.Devices.Abstractions;
using Application.Devices.Mapping;
using Application.Devices.Results;
using Application.Shared.Wrappers;
using Application.Tenants.Kiosks;

namespace Application.Devices.Queries;

public sealed class GetKioskHeartbeatsQueryHandler
{
    private readonly IKioskTelemetryStore _telemetryStore;

    public GetKioskHeartbeatsQueryHandler(IKioskTelemetryStore telemetryStore)
    {
        _telemetryStore = telemetryStore;
    }

    public async Task<PagedResult<KioskHeartbeatResult>> HandleAsync(
        GetKioskHeartbeatsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var kiosk = await _telemetryStore.GetKioskByIdAsync(query.KioskId, cancellationToken);
        if (kiosk is null)
        {
            return PagedResult<KioskHeartbeatResult>.Fail("Kiosk not found.", 404, pageNumber, pageSize);
        }

        if (!KioskAccessRules.CanAccessKiosk(query.UserContext, kiosk))
        {
            return PagedResult<KioskHeartbeatResult>.Forbidden("Access denied.", pageNumber, pageSize);
        }

        var totalCount = await _telemetryStore.CountHeartbeatsAsync(
            query.KioskId,
            query.Status,
            query.From,
            query.To,
            cancellationToken);

        var heartbeats = await _telemetryStore.ListHeartbeatsAsync(
            query.KioskId,
            query.Status,
            query.From,
            query.To,
            pageNumber,
            pageSize,
            cancellationToken);

        return PagedResult<KioskHeartbeatResult>.Success(
            heartbeats.Select(KioskTelemetryResultMapper.ToResult),
            totalCount,
            pageNumber,
            pageSize,
            "Kiosk heartbeats retrieved successfully.");
    }
}
