using Application.Orders.Management.Queries;
using Application.Orders.Management.Results;
using Domain.Orders.Enums;
using HotChocolate.Authorization;
using System.Security.Claims;
using WebAPI.Authorization;

namespace WebAPI.GraphQL.Orders;

[ExtendObjectType("Query")]
public sealed class OrderQueries
{
    [Authorize(Policy = "orders.view")]
    public async Task<ManagementOrdersPage> GetOrders(
        string? search,
        OrderStatus? status,
        PaymentStatus? paymentStatus,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        int? pageNumber,
        int? pageSize,
        ClaimsPrincipal claimsPrincipal,
        [Service] ListManagementOrdersQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ListManagementOrdersQuery
        {
            UserContext = claimsPrincipal.GetUserContext(),
            Search = search,
            Status = status,
            PaymentStatus = paymentStatus,
            OrganizationId = organizationId,
            StoreId = storeId,
            KioskId = kioskId,
            PageNumber = pageNumber ?? 1,
            PageSize = pageSize ?? 20
        }, cancellationToken);

        EnsureSucceeded(result, "Failed to retrieve orders.");
        return new ManagementOrdersPage(
            result.Data?.ToArray() ?? [],
            ToPageInfo(result.Pagination));
    }

    [Authorize(Policy = "orders.view")]
    public async Task<ManagementOrderDetailResult> GetOrder(
        Guid id,
        ClaimsPrincipal claimsPrincipal,
        [Service] GetManagementOrderQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetManagementOrderQuery
        {
            OrderId = id,
            UserContext = claimsPrincipal.GetUserContext()
        }, cancellationToken);

        EnsureSucceeded(result, "Failed to retrieve order.");
        return result.Data!;
    }

    [Authorize(Policy = "orders.view")]
    public async Task<OrderStatusHistoryPage> GetOrderStatusHistory(
        Guid orderId,
        int? pageNumber,
        int? pageSize,
        ClaimsPrincipal claimsPrincipal,
        [Service] GetOrderStatusHistoryQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetOrderStatusHistoryQuery
        {
            OrderId = orderId,
            UserContext = claimsPrincipal.GetUserContext(),
            PageNumber = pageNumber ?? 1,
            PageSize = pageSize ?? 20
        }, cancellationToken);

        EnsureSucceeded(result, "Failed to retrieve order status history.");
        return new OrderStatusHistoryPage(
            result.Data?.ToArray() ?? [],
            ToPageInfo(result.Pagination));
    }

    [Authorize(Policy = "orders.view")]
    public async Task<OrderExecutionAttemptsPage> GetOrderExecutionAttempts(
        Guid orderId,
        int? pageNumber,
        int? pageSize,
        ClaimsPrincipal claimsPrincipal,
        [Service] GetOrderExecutionAttemptsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetOrderExecutionAttemptsQuery
        {
            OrderId = orderId,
            UserContext = claimsPrincipal.GetUserContext(),
            PageNumber = pageNumber ?? 1,
            PageSize = pageSize ?? 20
        }, cancellationToken);

        EnsureSucceeded(result, "Failed to retrieve order execution attempts.");
        return new OrderExecutionAttemptsPage(
            result.Data?.ToArray() ?? [],
            ToPageInfo(result.Pagination));
    }

    [Authorize(Policy = "orders.view")]
    public async Task<OrderOverviewResult> GetOrderOverview(
        DateTimeOffset? from,
        DateTimeOffset? to,
        OrderStatus? status,
        Guid? kioskId,
        int take,
        ClaimsPrincipal claimsPrincipal,
        [Service] GetOrderOverviewQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var userContext = claimsPrincipal.GetUserContext();
        var query = new GetOrderOverviewQuery
        {
            UserContext = userContext,
            From = from,
            To = to,
            Status = status,
            KioskId = kioskId,
            Take = take
        };
        var result = await handler.HandleAsync(query, cancellationToken);

        if (!result.Succeeded)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(result.Message ?? "Failed to retrieve order overview.")
                    .SetCode(result.StatusCode.ToString())
                    .Build());
        }

        return result.Data!;
    }

    private static OrderPageInfo ToPageInfo(Application.Shared.Wrappers.PaginationMeta pagination) =>
        new(
            pagination.Page,
            pagination.PageSize,
            pagination.TotalCount,
            pagination.TotalPages,
            pagination.HasNext,
            pagination.HasPrevious);

    private static void EnsureSucceeded<T>(
        Application.Shared.Wrappers.ApiResult<T> result,
        string fallbackMessage)
    {
        if (result.Succeeded)
            return;

        throw new GraphQLException(
            ErrorBuilder.New()
                .SetMessage(result.Message ?? fallbackMessage)
                .SetCode(result.StatusCode.ToString())
                .Build());
    }
}
