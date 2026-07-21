using Application.Dashboard.Abstractions;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Dashboard.Queries;

public sealed class GetManagementDashboardQueryHandler
{
    private readonly IDashboardStore _dashboardStore;

    public GetManagementDashboardQueryHandler(IDashboardStore dashboardStore)
    {
        _dashboardStore = dashboardStore;
    }

    public async Task<ApiResult<DashboardMetrics>> HandleAsync(
        GetManagementDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        var userContext = query.UserContext;
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.DashboardView, userContext);
        var metrics = await _dashboardStore.GetDashboardMetricsAsync(
            userContext.IsSystemAdmin,
            scope.OrganizationIds,
            scope.StoreIds,
            scope.KioskIds,
            cancellationToken);

        return ApiResult<DashboardMetrics>.Success(metrics, "Dashboard metrics retrieved successfully.");
    }
}
