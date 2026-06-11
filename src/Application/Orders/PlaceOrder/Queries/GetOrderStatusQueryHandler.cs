using Application.Orders.Abstractions;
using Application.Orders.PlaceOrder.Mapping;
using Application.Orders.PlaceOrder.Results;
using Application.Shared.Wrappers;

namespace Application.Orders.PlaceOrder.Queries;

public sealed class GetOrderStatusQueryHandler
{
    private readonly IOrderStore _orderStore;

    public GetOrderStatusQueryHandler(IOrderStore orderStore)
    {
        _orderStore = orderStore;
    }

    public async Task<ApiResult<OrderResult>> HandleAsync(
        GetOrderStatusQuery query,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderStore.GetOrderByIdAsync(query.OrderId, cancellationToken);
        if (order is null)
        {
            return ApiResult<OrderResult>.Fail("Order not found.", 404);
        }

        return ApiResult<OrderResult>.Success(OrderResultMapper.ToResult(order));
    }
}
