using Application.Devices.Telemetry.Abstractions;
using Application.Devices.Connectivity.Abstractions;
using Application.Devices.Connectivity.Results;
using Application.Shared.Wrappers;

namespace Application.Devices.Connectivity.Queries;

public sealed class GetKioskStatusOverviewQueryHandler
{
    private readonly IKioskTelemetryStore _telemetryStore;

    public GetKioskStatusOverviewQueryHandler(IKioskTelemetryStore telemetryStore)
    {
        _telemetryStore = telemetryStore;
    }

    public async Task<ApiResult<KioskStatusOverviewResult>> HandleAsync(
        GetKioskStatusOverviewQuery query,
        CancellationToken cancellationToken = default)
    {
        var userContext = query.UserContext;

        var overview = await _telemetryStore.GetKioskStatusOverviewAsync(
            query.OrganizationId,
            query.StoreId,
            userContext.IsSystemAdmin,
            userContext.AllowedOrganizationIds,
            userContext.AllowedStoreIds,
            userContext.AllowedKioskIds,
            cancellationToken);

        return ApiResult<KioskStatusOverviewResult>.Success(overview, "Kiosk status overview retrieved successfully.");
    }
}
