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
    private readonly IRealtimeNotificationPublisher _publisher;

    public MarkOrderRefundRequiredCommandHandler(IOrderStore orderStore, IRealtimeNotificationPublisher publisher)
    {
        _orderStore = orderStore;
        _publisher = publisher;
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

        OrderStatus fromStatus = OrderStatus.Draft;

        var result = await _orderStore.ExecuteInTransactionAsync(async ct =>
        {
            var order = await _orderStore.GetOrderByIdAsync(command.OrderId, ct);
            if (order is null)
            {
                return ApiResult<OrderResult>.Fail("Order not found.", 404);
            }

            fromStatus = order.Status;

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

        if (result.Succeeded && result.Data is not null)
        {
            await _publisher.PublishOrderStatusChangedAsync(new OrderStatusChangedEvent
            {
                OrderId = result.Data.Id,
                OrderNumber = result.Data.OrderNumber,
                KioskId = result.Data.KioskId,
                OrganizationId = result.Data.OrganizationId,
                StoreId = result.Data.StoreId,
                OldStatus = fromStatus.ToString(),
                NewStatus = result.Data.Status.ToString(),
                PaymentStatus = result.Data.PaymentStatus.ToString(),
                CustomerStatus = result.Data.CustomerStatus,
                CustomerStatusMessage = result.Data.CustomerStatusMessage,
                CanRetryPayment = result.Data.CanRetryPayment,
                RequiresStaffSupport = result.Data.RequiresStaffSupport,
                UpdatedAt = DateTimeOffset.UtcNow,
                Version = 1
            }, cancellationToken);
        }

        return result;
    }
}
