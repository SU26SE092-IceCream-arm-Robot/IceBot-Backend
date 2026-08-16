using System.Text.Json;
using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.Identity.Tokens.Claims;
using Application.Orders.Support;
using Application.Payments.Abstractions;
using Application.Payments.PaymentSessions.Results;
using Application.Payments.PaymentSessions.Support;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Common;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using Domain.Payments.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Payments.PaymentSessions.Commands;

public sealed class ConfirmCashPaymentCommandHandler(
    IPaymentStore paymentStore,
    IRealtimeNotificationPublisher publisher,
    DispatchOrderExecutionCommandHandler dispatchOrderExecutionHandler,
    ILogger<ConfirmCashPaymentCommandHandler> logger)
{
    public async Task<ApiResult<CashPaymentConfirmationResult>> HandleAsync(
        ConfirmCashPaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.OrderId == Guid.Empty || command.PaymentTransactionId == Guid.Empty ||
            command.UserContext.AccountId == Guid.Empty)
        {
            return ApiResult<CashPaymentConfirmationResult>.Fail(
                "Order, cash payment, and confirming staff account are required.", 400);
        }

        var note = string.IsNullOrWhiteSpace(command.Request.Note) ? null : command.Request.Note.Trim();
        if (note?.Length > 500)
        {
            return ApiResult<CashPaymentConfirmationResult>.Fail("Cash payment note must not exceed 500 characters.", 400);
        }

        Order? confirmedOrder = null;
        OrderStatus? oldOrderStatus = null;
        PaymentTransactionStatus? oldPaymentStatus = null;
        Guid? confirmedPaymentTransactionId = null;
        var result = await paymentStore.ExecuteInTransactionAsync(async ct =>
        {
            await paymentStore.AcquirePaymentTransactionLockAsync(command.PaymentTransactionId, ct);
            var payment = await paymentStore.GetPaymentTransactionByIdAsync(command.PaymentTransactionId, ct);
            if (payment is null || payment.OrderId != command.OrderId)
            {
                return ApiResult<CashPaymentConfirmationResult>.Fail("Cash payment transaction not found.", 404);
            }

            await paymentStore.AcquireOrderWorkflowLockAsync(payment.OrderId, ct);
            await paymentStore.ReloadOrderAsync(payment.Order, ct);
            await paymentStore.AcquireKioskOperationalLockAsync(payment.Order.KioskId, ct);

            if (!ScopeAccessRules.CanAccessScopedRow(
                    ScopeRoleSets.CashPaymentsConfirm,
                    command.UserContext,
                    payment.Order.OrganizationId,
                    payment.Order.StoreId,
                    payment.Order.KioskId))
            {
                return ApiResult<CashPaymentConfirmationResult>.Fail("Cash payment transaction not found.", 404);
            }

            if (!CashPaymentMethodResolver.IsCash(payment.PaymentMethod.Code) ||
                !string.Equals(payment.Provider, CashPaymentMethodResolver.ProviderCode, StringComparison.OrdinalIgnoreCase))
            {
                return ApiResult<CashPaymentConfirmationResult>.Fail("Payment transaction is not a cash payment.", 409);
            }

            if (payment.Status == PaymentTransactionStatus.Paid)
            {
                return ApiResult<CashPaymentConfirmationResult>.Success(new CashPaymentConfirmationResult
                {
                    OrderId = payment.OrderId,
                    PaymentTransactionId = payment.Id,
                    PaymentStatus = payment.Status,
                    AlreadyConfirmed = true
                }, "Cash payment was already confirmed.");
            }

            if (payment.Status is not (PaymentTransactionStatus.Pending or PaymentTransactionStatus.Authorized))
            {
                return ApiResult<CashPaymentConfirmationResult>.Fail(
                    "Cash payment cannot be confirmed in its current state.", 409);
            }

            var now = DateTimeOffset.UtcNow;
            if (payment.Order.PaymentDeadlineAt != default && payment.Order.PaymentDeadlineAt <= now)
            {
                payment.MarkExpired(now);
                if (payment.Order.PaymentStatus is not (PaymentStatus.Paid or PaymentStatus.PartiallyRefunded or PaymentStatus.Refunded))
                {
                    payment.Order.MarkPaymentCancelled();
                }

                await paymentStore.SaveChangesAsync(ct);
                return ApiResult<CashPaymentConfirmationResult>.Fail("The order payment window has expired.", 409);
            }

            var appliedSettlement = await paymentStore.GetAppliedPaymentSettlementByOrderIdAsync(payment.OrderId, ct);
            if (appliedSettlement is not null && appliedSettlement.Id != payment.Id)
            {
                return ApiResult<CashPaymentConfirmationResult>.Fail(
                    "Order already has an applied payment settlement. Cash confirmation requires payment review.", 409);
            }

            oldOrderStatus = payment.Order.Status;
            oldPaymentStatus = payment.Status;
            try
            {
                payment.PaidAmount = payment.Amount;
                payment.ProviderPaidAt = now;
                payment.ProviderStatus = "ConfirmedByStaff";
                payment.MarkPaid($"cash:{payment.Id:N}", now, JsonSerializer.Serialize(new
                {
                    eventType = "CashPaymentConfirmed",
                    confirmedByAccountId = command.UserContext.AccountId,
                    confirmedAt = now,
                    note
                }));
                payment.AssignPrimarySettlement();
                payment.Order.MarkPaid(payment.Amount, now);
            }
            catch (DomainRuleException exception)
            {
                return ApiResult<CashPaymentConfirmationResult>.Fail(exception.Message, 409);
            }

            if (payment.Order.Status != oldOrderStatus)
            {
                await paymentStore.AddOrderStatusHistoryAsync(new OrderStatusHistory
                {
                    Id = Guid.NewGuid(),
                    OrderId = payment.Order.Id,
                    FromStatus = oldOrderStatus.Value,
                    ToStatus = payment.Order.Status,
                    ChangedAt = now,
                    ChangedByAccountId = command.UserContext.AccountId,
                    Reason = string.IsNullOrWhiteSpace(note)
                        ? "Cash payment confirmed by staff."
                        : $"Cash payment confirmed by staff. {note}"
                }, ct);
            }

            payment.Order.UpdatedAt = now;
            payment.Order.UpdatedByAccountId = command.UserContext.AccountId;
            await paymentStore.SaveChangesAsync(ct);
            confirmedOrder = payment.Order;
            confirmedPaymentTransactionId = payment.Id;
            return ApiResult<CashPaymentConfirmationResult>.Success(new CashPaymentConfirmationResult
            {
                OrderId = payment.OrderId,
                PaymentTransactionId = payment.Id,
                PaymentStatus = payment.Status,
                AlreadyConfirmed = false
            }, "Cash payment confirmed.");
        }, cancellationToken);

        if (!result.Succeeded || result.Data is null || confirmedOrder is null)
        {
            return result;
        }

        await PublishConfirmationAsync(
            confirmedOrder,
            confirmedPaymentTransactionId!.Value,
            oldOrderStatus,
            oldPaymentStatus,
            cancellationToken);
        return result;
    }

    private async Task PublishConfirmationAsync(
        Order order,
        Guid paymentTransactionId,
        OrderStatus? oldOrderStatus,
        PaymentTransactionStatus? oldPaymentStatus,
        CancellationToken cancellationToken)
    {
        if (oldPaymentStatus.HasValue)
        {
            await publisher.PublishPaymentStatusChangedAsync(new PaymentStatusChangedEvent
            {
                OrderId = order.Id,
                PaymentTransactionId = paymentTransactionId,
                OldStatus = oldPaymentStatus.Value.ToString(),
                NewStatus = PaymentTransactionStatus.Paid.ToString(),
                Provider = CashPaymentMethodResolver.ProviderCode,
                UpdatedAt = DateTimeOffset.UtcNow,
                Version = 1,
                OrganizationId = order.OrganizationId,
                StoreId = order.StoreId
            }, cancellationToken);
        }

        if (oldOrderStatus.HasValue && oldOrderStatus != order.Status)
        {
            var projection = OrderStatusProjector.ProjectFromOrder(order);
            await publisher.PublishOrderStatusChangedAsync(new OrderStatusChangedEvent
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                KioskId = order.KioskId,
                OrganizationId = order.OrganizationId,
                StoreId = order.StoreId,
                OldStatus = oldOrderStatus.Value.ToString(),
                NewStatus = order.Status.ToString(),
                PaymentStatus = order.PaymentStatus.ToString(),
                CustomerStatus = projection.CustomerStatus,
                CustomerStatusMessage = projection.CustomerStatusMessage,
                CanRetryPayment = projection.CanRetryPayment,
                RequiresStaffSupport = projection.RequiresStaffSupport,
                UpdatedAt = order.UpdatedAt ?? DateTimeOffset.UtcNow,
                Version = 1
            }, cancellationToken);
        }

        if (order.Status == OrderStatus.ReadyForFulfillment &&
            order.OrderItems.Any(item => item.FulfillmentType == Domain.Catalog.Enums.FulfillmentType.MachineProduced))
        {
            var dispatch = await dispatchOrderExecutionHandler.HandleAsync(
                new DispatchOrderExecutionCommand { OrderId = order.Id, DispatchAttemptNo = 1 },
                cancellationToken);
            if (!dispatch.Succeeded)
            {
                logger.LogWarning(
                    "Cash-paid order {OrderId} remains ready for execution because command dispatch was deferred: {Message}",
                    order.Id,
                    dispatch.Message);
            }
        }
    }
}
