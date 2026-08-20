using Application.Shared.Wrappers;
using Application.Tenants;

namespace Application.Payments.Reconciliation;

public sealed class GetDailyPaymentReconciliationQueryHandler
{
    private readonly IPaymentReconciliationStore _store;
    private readonly PaymentReconciliationOptions _options;

    public GetDailyPaymentReconciliationQueryHandler(IPaymentReconciliationStore store, PaymentReconciliationOptions options)
    {
        _store = store;
        _options = options;
    }

    public async Task<ApiResult<DailyPaymentReconciliationResult>> HandleAsync(
        DailyPaymentReconciliationQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Date == default)
        {
            return ApiResult<DailyPaymentReconciliationResult>.Fail("date is required.", 400);
        }

        var request = BuildReadRequest(query, _options);
        return ApiResult<DailyPaymentReconciliationResult>.Success(
            await _store.GetDailySummaryAsync(request, query.Date, _options.TimeZoneId, cancellationToken));
    }

    internal static PaymentReconciliationReadRequest BuildReadRequest(
        DailyPaymentReconciliationQuery query, PaymentReconciliationOptions options)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId);
        var start = TimeZoneInfo.ConvertTimeToUtc(query.Date.ToDateTime(TimeOnly.MinValue), timeZone);
        var end = start.AddDays(1);
        var scope = ScopeAccessRules.GetEffectiveScope(ScopeRoleSets.PaymentReconciliationView, query.UserContext);
        return new PaymentReconciliationReadRequest(
            new DateTimeOffset(start, TimeSpan.Zero), new DateTimeOffset(end, TimeSpan.Zero),
            DateTimeOffset.UtcNow.AddMinutes(-options.EvidenceFreshnessMinutes),
            string.IsNullOrWhiteSpace(query.Provider) ? "PayOS" : query.Provider.Trim(),
            query.OrganizationId, query.StoreId, query.KioskId,
            query.UserContext.IsSystemAdmin,
            scope.OrganizationIds.ToArray(), scope.StoreIds.ToArray(), scope.KioskIds.ToArray());
    }
}

public sealed class ListPaymentReconciliationDiscrepanciesQueryHandler
{
    private readonly IPaymentReconciliationStore _store;
    private readonly PaymentReconciliationOptions _options;

    public ListPaymentReconciliationDiscrepanciesQueryHandler(IPaymentReconciliationStore store, PaymentReconciliationOptions options)
    {
        _store = store;
        _options = options;
    }

    public async Task<PagedResult<PaymentReconciliationDiscrepancyResult>> HandleAsync(
        PaymentReconciliationDiscrepanciesQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Date == default)
        {
            return PagedResult<PaymentReconciliationDiscrepancyResult>.Fail("date is required.", 400, query.PageNumber, query.PageSize);
        }

        var request = GetDailyPaymentReconciliationQueryHandler.BuildReadRequest(query, _options);
        var page = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var result = await _store.ListDiscrepanciesAsync(request, page, pageSize, cancellationToken);
        return PagedResult<PaymentReconciliationDiscrepancyResult>.Success(result.Items, result.TotalCount, page, pageSize);
    }
}
