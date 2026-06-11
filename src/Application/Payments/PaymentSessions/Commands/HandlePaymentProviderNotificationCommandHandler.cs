using Application.Payments.Abstractions;
using Application.Payments.PaymentSessions.Results;
using Application.Payments.PaymentSessions.Support;
using Application.Payments.Providers;
using Application.Shared.Wrappers;
using Domain.Payments.Entities;
using Domain.Payments.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Payments.PaymentSessions.Commands;

public sealed class HandlePaymentProviderNotificationCommandHandler
{
    private readonly IPaymentStore _paymentStore;
    private readonly IPaymentGateway _paymentGateway;

    public HandlePaymentProviderNotificationCommandHandler(IPaymentStore paymentStore, IPaymentGateway paymentGateway)
    {
        _paymentStore = paymentStore;
        _paymentGateway = paymentGateway;
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
        catch (Exception ex)
        {
            return ApiResult<PaymentNotificationResult>.Fail(ex.Message, 400);
        }

        return await _paymentStore.ExecuteInTransactionAsync(async ct =>
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

            var originalStatus = paymentTransaction.Order.Status;
            var alreadyProcessed = paymentTransaction.Status == PaymentTransactionStatus.Paid;
            if (!alreadyProcessed)
            {
                PaymentNotificationApplier.ApplyNotification(paymentTransaction, notification);

                if (paymentTransaction.Order.Status != originalStatus)
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
    }
}
