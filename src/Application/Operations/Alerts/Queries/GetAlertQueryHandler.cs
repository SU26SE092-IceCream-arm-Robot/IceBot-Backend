using Application.Operations.Abstractions;
using Application.Operations.Alerts.Mapping;
using Application.Operations.Alerts.Results;
using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Operations.Alerts.Queries;

public sealed class GetAlertQueryHandler
{
    private readonly IAlertStore _store;

    public GetAlertQueryHandler(IAlertStore store)
    {
        _store = store;
    }

    public async Task<ApiResult<AlertResult>> HandleAsync(
        GetAlertQuery query,
        CancellationToken cancellationToken = default)
    {
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.AlertsView, query.UserContext);
        var alert = await _store.GetAccessibleByIdAsync(
            query.AlertId,
            query.UserContext.IsSystemAdmin,
            scope.OrganizationIds,
            scope.StoreIds,
            scope.KioskIds,
            cancellationToken);
        if (alert is null)
        {
            return ApiResult<AlertResult>.Fail("Alert not found.", 404);
        }

        return ApiResult<AlertResult>.Success(AlertResultMapper.ToResult(alert));
    }
}
