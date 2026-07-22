using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.EdgeIntegration.Abstractions;
using Application.Orders.Support;
using Application.Shared.Wrappers;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Microsoft.Extensions.Options;

namespace Application.EdgeIntegration.Dispatch.Commands;

public sealed class EscalateInitialDispatchFailureCommandHandler(
    IOrderExecutionDispatchStore store,
    IRealtimeNotificationPublisher publisher,
    IOptions<OrderExecutionDispatchOptions> options)
{
    public async Task<ApiResult<bool>> HandleAsync(
        Guid orderId,
        string failureReason,
        CancellationToken cancellationToken = default)
    {
        OrderStatusChangedEvent? statusChangedEvent = null;
        var result = await store.ExecuteSerializedAsync(orderId, async ct =>
        {
            var order = await store.GetOrderAsync(orderId, ct);
            if (order is null)
                return ApiResult<bool>.Fail("Order not found.", 404);

            var escalationCutoff = DateTimeOffset.UtcNow.AddMinutes(
                -options.Value.InitialDispatchSupportEscalationMinutes);
            if (order.PaymentStatus != PaymentStatus.Paid ||
                order.Status != OrderStatus.ReadyForFulfillment ||
                !order.PaidAt.HasValue ||
                order.PaidAt.Value > escalationCutoff ||
                await store.GetCommandAsync(order.Id, 1, ct) is not null)
            {
                return ApiResult<bool>.Success(false);
            }

            var changedAt = DateTimeOffset.UtcNow;
            var oldStatus = order.Status;
            var reason = $"Initial machine dispatch could not be created within the support SLA: {failureReason}";
            order.MarkFulfillmentIssue(reason);
            order.UpdatedAt = changedAt;
            await store.AddOrderStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = oldStatus,
                ToStatus = order.Status,
                ChangedAt = changedAt,
                Reason = reason
            }, ct);
            await store.SaveChangesAsync(ct);

            var projection = OrderStatusProjector.ProjectFromOrder(order);
            statusChangedEvent = new OrderStatusChangedEvent
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                KioskId = order.KioskId,
                OrganizationId = order.OrganizationId,
                StoreId = order.StoreId,
                OldStatus = oldStatus.ToString(),
                NewStatus = order.Status.ToString(),
                PaymentStatus = order.PaymentStatus.ToString(),
                CustomerStatus = projection.CustomerStatus,
                CustomerStatusMessage = projection.CustomerStatusMessage,
                CanRetryPayment = projection.CanRetryPayment,
                RequiresStaffSupport = projection.RequiresStaffSupport,
                UpdatedAt = changedAt,
                Version = 1
            };
            return ApiResult<bool>.Success(true);
        }, cancellationToken);

        if (result.Succeeded && result.Data && statusChangedEvent is not null)
            await publisher.PublishOrderStatusChangedAsync(statusChangedEvent, cancellationToken);

        return result;
    }
}
