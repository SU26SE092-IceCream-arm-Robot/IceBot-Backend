using Application.Orders.Abstractions;
using Application.Orders.PlaceOrder.Mapping;
using Application.Orders.PlaceOrder.Results;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Orders.Entities;
using Domain.Orders.Enums;

namespace Application.Orders.Management.Commands;

public sealed class MarkOrderRefundRequiredCommandHandler
{
    private readonly IOrderStore _orderStore;

    public MarkOrderRefundRequiredCommandHandler(IOrderStore orderStore)
    {
        _orderStore = orderStore;
    }

    public async Task<ApiResult<OrderResult>> HandleAsync(
        MarkOrderRefundRequiredCommand command,
        CancellationToken cancellationToken = default)
    {
        var reason = command.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            return ApiResult<OrderResult>.Fail("Reason is required to flag an order as refund required.", 400);
        }

        return await _orderStore.ExecuteInTransactionAsync(async ct =>
        {
            var order = await _orderStore.GetOrderByIdAsync(command.OrderId, ct);
            if (order is null)
            {
                return ApiResult<OrderResult>.Fail("Order not found.", 404);
            }

            if (!ScopeAccessRules.CanAccessScopedRow(
                command.UserContext,
                order.OrganizationId,
                order.StoreId,
                order.KioskId))
            {
                return ApiResult<OrderResult>.Fail("Access denied.", 403);
            }

            if (order.PaymentStatus != PaymentStatus.Paid)
            {
                return ApiResult<OrderResult>.Fail(
                    "Only paid orders can require refund. For unpaid orders, please cancel them directly.",
                    409);
            }

            if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            {
                return ApiResult<OrderResult>.Fail(
                    $"Cannot flag completed or cancelled order as refund required.",
                    409);
            }

            var fromStatus = order.Status;
            var now = DateTimeOffset.UtcNow;

            order.MarkRefundRequired(reason);
            order.UpdatedAt = now;

            var history = new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                FromStatus = fromStatus,
                ToStatus = OrderStatus.RefundRequired,
                ChangedByAccountId = command.UserContext.AccountId,
                Reason = reason,
                ChangedAt = now
            };
            await _orderStore.AddOrderStatusHistoryAsync(history, ct);

            await _orderStore.SaveChangesAsync(ct);

            return ApiResult<OrderResult>.Success(
                OrderResultMapper.ToResult(order),
                "Order flagged as refund required successfully.");
        }, cancellationToken);
    }
}
