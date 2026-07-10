using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Payments.Abstractions;
using Application.Payments.PaymentSessions.Results;
using Application.Payments.PaymentSessions.Support;
using Application.Payments.Providers;
using Application.Shared.Exceptions;
using Application.Shared.Wrappers;
using Domain.Orders.Enums;
using Domain.Payments.Entities;
using Domain.Payments.Enums;
using Application.EdgeIntegration.CommandDelivery.Commands;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.EdgeIntegration.Reports.Commands;
using Application.EdgeIntegration.Timeouts.Commands;
using Microsoft.Extensions.Logging;

namespace Application.Payments.PaymentSessions.Commands;

public sealed class HandlePaymentProviderNotificationCommandHandler
{
    private readonly IPaymentStore _paymentStore;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IRealtimeNotificationPublisher _publisher;
    private readonly DispatchOrderExecutionCommandHandler _dispatchOrderExecutionHandler;
    private readonly ILogger<HandlePaymentProviderNotificationCommandHandler> _logger;

    public HandlePaymentProviderNotificationCommandHandler(
        IPaymentStore paymentStore,
        IPaymentGateway paymentGateway,
        IRealtimeNotificationPublisher publisher,
        DispatchOrderExecutionCommandHandler dispatchOrderExecutionHandler,
        ILogger<HandlePaymentProviderNotificationCommandHandler> logger)
    {
        _paymentStore = paymentStore;
        _paymentGateway = paymentGateway;
        _publisher = publisher;
        _dispatchOrderExecutionHandler = dispatchOrderExecutionHandler;
        _logger = logger;
    }

    public async Task<ApiResult<PaymentNotificationResult>> HandleAsync(
        HandlePaymentProviderNotificationCommand command,
        CancellationToken cancellationToken = default)
    {
        var request = command.Request;

        if (string.IsNullOrWhiteSpace(request.RawPayload))
        {
            return ApiResult<PaymentNotificationResult>.Fail("Webhook payload is required.", 400);
        }

        ProviderPaymentNotification notification;
        try
        {
            notification = await _paymentGateway.ParseAndVerifyNotificationAsync(
                request.RawPayload,
                request.Signature,
                cancellationToken);
        }
        catch (AppException ex)
        {
            return ApiResult<PaymentNotificationResult>.Fail(ex.Message, ex.StatusCode);
        }
        catch (Exception ex)
        {
            return ApiResult<PaymentNotificationResult>.Fail(ex.Message, 400);
        }

        OrderStatus originalStatus = OrderStatus.Draft;
        OrderStatus newOrderStatus = OrderStatus.Draft;
        PaymentTransactionStatus originalTxStatus = PaymentTransactionStatus.Pending;
        PaymentTransactionStatus newTxStatus = PaymentTransactionStatus.Pending;
        bool statusChanged = false;
        bool txStatusChanged = false;

        Guid? orgId = null;
        Guid? storeId = null;
        Guid kioskId = Guid.Empty;
        string orderNumber = "";
        string provider = "";
        string customerStatus = "";
        string customerStatusMessage = "";
        string orderPaymentStatus = "";
        bool canRetryPayment = false;
        bool requiresStaffSupport = false;

        var result = await _paymentStore.ExecuteInTransactionAsync(async ct =>
        {
            if (!string.IsNullOrWhiteSpace(notification.ProviderEventId) &&
                await _paymentStore.PaymentCallbackExistsAsync(notification.Provider, notification.ProviderEventId, ct))
            {
                var existingPayment = !string.IsNullOrWhiteSpace(notification.ProviderOrderCode)
                    ? await _paymentStore.GetPaymentTransactionByProviderOrderCodeAsync(
                        notification.Provider,
                        notification.ProviderOrderCode,
                        ct)
                    : null;

                if (existingPayment is null)
                {
                    return ApiResult<PaymentNotificationResult>.Fail("Duplicate webhook ignored, but payment transaction was not found.", 404);
                }

                return ApiResult<PaymentNotificationResult>.Success(new PaymentNotificationResult
                {
                    PaymentTransactionId = existingPayment.Id,
                    OrderId = existingPayment.OrderId,
                    Status = existingPayment.Status,
                    AlreadyProcessed = true
                });
            }

            PaymentTransaction? paymentTransaction = null;

            if (!string.IsNullOrWhiteSpace(notification.ProviderOrderCode))
            {
                paymentTransaction = await _paymentStore.GetPaymentTransactionByProviderOrderCodeAsync(
                    notification.Provider,
                    notification.ProviderOrderCode,
                    ct);
            }

            if (paymentTransaction is null)
            {
                return ApiResult<PaymentNotificationResult>.Fail("Payment transaction not found.", 404);
            }

            var callback = new PaymentCallback
            {
                PaymentTransactionId = paymentTransaction.Id,
                Provider = notification.Provider,
                EventType = notification.EventType,
                ProviderEventId = notification.ProviderEventId,
                PayloadJson = notification.RawPayloadJson,
                Signature = request.Signature,
                ReceivedAt = DateTimeOffset.UtcNow
            };

            await _paymentStore.AddPaymentCallbackAsync(callback, ct);

            originalStatus = paymentTransaction.Order.Status;
            originalTxStatus = paymentTransaction.Status;
            var alreadyProcessed = paymentTransaction.Status == PaymentTransactionStatus.Paid;
            if (!alreadyProcessed)
            {
                PaymentNotificationApplier.ApplyNotification(paymentTransaction, notification);

                newOrderStatus = paymentTransaction.Order.Status;
                newTxStatus = paymentTransaction.Status;
                statusChanged = newOrderStatus != originalStatus;
                txStatusChanged = newTxStatus != originalTxStatus;

                orgId = paymentTransaction.Order.OrganizationId;
                storeId = paymentTransaction.Order.StoreId;
                kioskId = paymentTransaction.Order.KioskId;
                orderNumber = paymentTransaction.Order.OrderNumber;
                provider = paymentTransaction.Provider;
                orderPaymentStatus = paymentTransaction.Order.PaymentStatus.ToString();

                var customerStatusInfo = Application.Orders.Support.OrderStatusProjector.ProjectFromOrder(paymentTransaction.Order);
                customerStatus = customerStatusInfo.CustomerStatus;
                customerStatusMessage = customerStatusInfo.CustomerStatusMessage;
                canRetryPayment = customerStatusInfo.CanRetryPayment;
                requiresStaffSupport = customerStatusInfo.RequiresStaffSupport;

                if (statusChanged)
                {
                    var history = new Domain.Orders.Entities.OrderStatusHistory
                    {
                        Id = Guid.NewGuid(),
                        OrderId = paymentTransaction.Order.Id,
                        FromStatus = originalStatus,
                        ToStatus = paymentTransaction.Order.Status,
                        ChangedAt = DateTimeOffset.UtcNow,
                        Reason = "Payment webhook notification received."
                    };
                    await _paymentStore.AddOrderStatusHistoryAsync(history, ct);
                }
            }

            callback.MarkProcessed(DateTimeOffset.UtcNow);
            await _paymentStore.SaveChangesAsync(ct);

            return ApiResult<PaymentNotificationResult>.Success(new PaymentNotificationResult
            {
                PaymentTransactionId = paymentTransaction.Id,
                OrderId = paymentTransaction.OrderId,
                Status = paymentTransaction.Status,
                AlreadyProcessed = alreadyProcessed
            });
        }, cancellationToken);

        if (result.Succeeded && result.Data is not null)
        {
            if (txStatusChanged)
            {
                await _publisher.PublishPaymentStatusChangedAsync(new PaymentStatusChangedEvent
                {
                    OrderId = result.Data.OrderId,
                    PaymentTransactionId = result.Data.PaymentTransactionId,
                    OldStatus = originalTxStatus.ToString(),
                    NewStatus = newTxStatus.ToString(),
                    Provider = provider,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    Version = 1,
                    OrganizationId = orgId,
                    StoreId = storeId
                }, cancellationToken);
            }

            if (statusChanged)
            {
                await _publisher.PublishOrderStatusChangedAsync(new OrderStatusChangedEvent
                {
                    OrderId = result.Data.OrderId,
                    OrderNumber = orderNumber,
                    KioskId = kioskId,
                    OrganizationId = orgId,
                    StoreId = storeId,
                    OldStatus = originalStatus.ToString(),
                    NewStatus = newOrderStatus.ToString(),
                    PaymentStatus = orderPaymentStatus,
                    CustomerStatus = customerStatus,
                    CustomerStatusMessage = customerStatusMessage,
                    CanRetryPayment = canRetryPayment,
                    RequiresStaffSupport = requiresStaffSupport,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    Version = 1
                }, cancellationToken);
            }

            if (result.Data.Status == PaymentTransactionStatus.Paid)
            {
                var dispatchResult = await _dispatchOrderExecutionHandler.HandleAsync(
                    new DispatchOrderExecutionCommand
                    {
                        OrderId = result.Data.OrderId,
                        DispatchAttemptNo = 1
                    },
                    cancellationToken);

                if (!dispatchResult.Succeeded)
                {
                    _logger.LogWarning(
                        "Paid order {OrderId} remains ready for execution because command dispatch was deferred: {Message}",
                        result.Data.OrderId,
                        dispatchResult.Message);
                }
            }
        }

        return result;
    }
}
