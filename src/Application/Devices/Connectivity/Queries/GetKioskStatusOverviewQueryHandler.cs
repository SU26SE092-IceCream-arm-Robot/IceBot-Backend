using Application.Devices.Telemetry.Abstractions;
using Application.Devices.Connectivity.Abstractions;
using Application.Devices.Connectivity.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

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
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.OperationsView, userContext);

        var overview = await _telemetryStore.GetKioskStatusOverviewAsync(
            query.OrganizationId,
            query.StoreId,
            userContext.IsSystemAdmin,
            scope.OrganizationIds,
            scope.StoreIds,
            scope.KioskIds,
            cancellationToken);

        return ApiResult<KioskStatusOverviewResult>.Success(overview, "Kiosk status overview retrieved successfully.");
    }
}
