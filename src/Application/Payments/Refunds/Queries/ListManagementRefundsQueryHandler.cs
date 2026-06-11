using Application.Payments.Abstractions;
using Application.Payments.Refunds.Mapping;
using Application.Payments.Refunds.Results;
using Application.Shared.Wrappers;

namespace Application.Payments.Refunds.Queries;

public sealed class ListManagementRefundsQueryHandler
{
    private readonly IPaymentStore _paymentStore;

    public ListManagementRefundsQueryHandler(IPaymentStore paymentStore)
    {
        _paymentStore = paymentStore;
    }

    public async Task<PagedResult<RefundResult>> HandleAsync(
        ListManagementRefundsQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var totalCount = await _paymentStore.CountRefundsAsync(
            query.Search,
            query.Status,
            query.OrganizationId,
            query.StoreId,
            query.KioskId,
            query.UserContext.IsSystemAdmin,
            query.UserContext.AllowedOrganizationIds,
            query.UserContext.AllowedStoreIds,
            query.UserContext.AllowedKioskIds,
            cancellationToken);

        var refunds = await _paymentStore.ListRefundsAsync(
            query.Search,
            query.Status,
            query.OrganizationId,
            query.StoreId,
            query.KioskId,
            query.UserContext.IsSystemAdmin,
            query.UserContext.AllowedOrganizationIds,
            query.UserContext.AllowedStoreIds,
            query.UserContext.AllowedKioskIds,
            pageNumber,
            pageSize,
            cancellationToken);

        return PagedResult<RefundResult>.Success(
            refunds.Select(RefundResultMapper.ToResult),
            totalCount,
            pageNumber,
            pageSize,
            "Refunds retrieved successfully.");
    }
}
