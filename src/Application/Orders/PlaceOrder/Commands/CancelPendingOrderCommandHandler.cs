using Application.Orders.Abstractions;
using Application.Orders.PlaceOrder.Mapping;
using Application.Orders.PlaceOrder.Results;
using Application.Shared.Wrappers;
using Domain.Orders.Enums;
using Domain.Orders.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Orders.PlaceOrder.Commands;

public sealed class CancelPendingOrderCommandHandler
{
    private readonly IOrderStore _orderStore;

    public CancelPendingOrderCommandHandler(IOrderStore orderStore)
    {
        _orderStore = orderStore;
    }

    public async Task<ApiResult<OrderResult>> HandleAsync(
        CancelPendingOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var orderId = command.OrderId;
        var request = command.Request;

        return await _orderStore.ExecuteInTransactionAsync(async ct =>
        {
            var order = await _orderStore.GetOrderByIdAsync(orderId, ct);
            if (order is null)
            {
                return ApiResult<OrderResult>.Fail("Order not found.", 404);
            }

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                return ApiResult<OrderResult>.Fail("Paid orders cannot be cancelled through this endpoint.", 409);
            }

            if (order.Status is not (OrderStatus.Draft or OrderStatus.PendingPayment))
            {
                return ApiResult<OrderResult>.Fail("Only draft or pending-payment orders can be cancelled.", 409);
            }

            var fromStatus = order.Status;
            var now = DateTimeOffset.UtcNow;
            var reason = NormalizeOptional(request.Reason);
            
            order.Cancel(now, reason);
            order.PaymentStatus = PaymentStatus.Cancelled;
            order.UpdatedAt = now;

            var history = new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                FromStatus = fromStatus,
                ToStatus = Domain.Orders.Enums.OrderStatus.Cancelled,
                ChangedAt = now,
                Reason = reason ?? "Order cancelled by customer."
            };
            await _orderStore.AddOrderStatusHistoryAsync(history, ct);

            await _orderStore.SaveChangesAsync(ct);

            return ApiResult<OrderResult>.Success(OrderResultMapper.ToResult(order), "Order cancelled.");
        }, cancellationToken);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
