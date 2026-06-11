using Application.Orders.Abstractions;
using Application.Orders.PlaceOrder.Mapping;
using Application.Orders.PlaceOrder.Results;
using Application.Shared.Wrappers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Orders.Management.Queries;

public sealed class ListManagementOrdersQueryHandler
{
    private readonly IOrderStore _orderStore;

    public ListManagementOrdersQueryHandler(IOrderStore orderStore)
    {
        _orderStore = orderStore;
    }

    public async Task<PagedResult<OrderResult>> HandleAsync(
        ListManagementOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var totalCount = await _orderStore.CountOrdersAsync(
            query.Search,
            query.Status,
            query.PaymentStatus,
            query.OrganizationId,
            query.StoreId,
            query.KioskId,
            query.UserContext.IsSystemAdmin,
            query.UserContext.AllowedOrganizationIds,
            query.UserContext.AllowedStoreIds,
            query.UserContext.AllowedKioskIds,
            cancellationToken);

        var orders = await _orderStore.ListOrdersAsync(
            query.Search,
            query.Status,
            query.PaymentStatus,
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

        return PagedResult<OrderResult>.Success(
            orders.Select(OrderResultMapper.ToResult),
            totalCount,
            pageNumber,
            pageSize,
            "Orders retrieved successfully.");
    }
}
