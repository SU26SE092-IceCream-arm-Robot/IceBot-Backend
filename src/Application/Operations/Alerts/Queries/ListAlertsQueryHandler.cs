using Application.Operations.Abstractions;
using Application.Operations.Alerts.Mapping;
using Application.Operations.Alerts.Results;
using Application.Shared.Wrappers;
using Domain.Common.Enums;
using Domain.Operations.Enums;
using Application.Tenants;

namespace Application.Operations.Alerts.Queries;

public sealed class ListAlertsQueryHandler
{
    private readonly IAlertStore _store;

    public ListAlertsQueryHandler(IAlertStore store)
    {
        _store = store;
    }

    public async Task<PagedResult<AlertResult>> HandleAsync(
        ListAlertsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        if (!TryParse(query.Status, out AlertStatus? status))
        {
            return PagedResult<AlertResult>.Fail("Invalid alert status.", 400, pageNumber, pageSize);
        }

        if (!TryParse(query.Severity, out SeverityLevel? severity))
        {
            return PagedResult<AlertResult>.Fail("Invalid alert severity.", 400, pageNumber, pageSize);
        }

        if (query.From.HasValue && query.To.HasValue && query.From > query.To)
        {
            return PagedResult<AlertResult>.Fail("Alert from timestamp cannot be after to timestamp.", 400, pageNumber, pageSize);
        }

        var user = query.UserContext;
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.AlertsView, user);
        var total = await _store.CountAsync(
            status, severity, query.OrganizationId, query.StoreId, query.KioskId, query.DeviceId,
            query.From, query.To, user.IsSystemAdmin,
            scope.OrganizationIds, scope.StoreIds, scope.KioskIds, cancellationToken);
        var alerts = await _store.ListAsync(
            status, severity, query.OrganizationId, query.StoreId, query.KioskId, query.DeviceId,
            query.From, query.To, user.IsSystemAdmin,
            scope.OrganizationIds, scope.StoreIds, scope.KioskIds,
            pageNumber, pageSize, cancellationToken);

        return PagedResult<AlertResult>.Success(
            alerts.Select(AlertResultMapper.ToResult).ToList(), total, pageNumber, pageSize);
    }

    private static bool TryParse<TEnum>(string? value, out TEnum? parsed)
        where TEnum : struct, Enum
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!Enum.TryParse<TEnum>(value.Trim(), true, out var result) || !Enum.IsDefined(result))
        {
            return false;
        }

        parsed = result;
        return true;
    }
}
