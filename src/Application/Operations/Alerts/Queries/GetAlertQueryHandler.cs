using Application.Operations.Abstractions;
using Application.Operations.Alerts.Mapping;
using Application.Operations.Alerts.Results;
using Application.Operations.Alerts.Rules;
using Application.Shared.Wrappers;

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
        var alert = await _store.GetByIdAsync(query.AlertId, cancellationToken);
        if (alert is null)
        {
            return ApiResult<AlertResult>.Fail("Alert not found.", 404);
        }

        if (!AlertAccessRules.CanAccess(
                query.UserContext, alert.Kiosk.OrganizationId, alert.Kiosk.StoreId, alert.KioskId))
        {
            return ApiResult<AlertResult>.Fail("Access denied.", 403);
        }

        return ApiResult<AlertResult>.Success(AlertResultMapper.ToResult(alert));
    }
}
