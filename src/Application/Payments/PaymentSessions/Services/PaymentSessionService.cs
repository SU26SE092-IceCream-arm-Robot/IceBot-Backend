using Application.Payments.Abstractions;
using Application.Payments.PaymentSessions.Requests;
using Application.Payments.PaymentSessions.Results;
using Application.Payments.Providers;
using Application.Shared.Wrappers;
using Domain.Orders.Enums;
using Domain.Payments.Entities;
using Domain.Payments.Enums;
using System.Text.Json;

namespace Application.Payments.PaymentSessions.Services;

public sealed class PaymentSessionService
{
    private const string PayOsMethodCode = "payos";

    private readonly IPaymentStore _paymentStore;
    private readonly IPaymentGateway _paymentGateway;

    public PaymentSessionService(IPaymentStore paymentStore, IPaymentGateway paymentGateway)
    {
        _paymentStore = paymentStore;
        _paymentGateway = paymentGateway;
    }

    public async Task<ApiResult<PaymentSessionResult>> CreatePaymentSessionAsync(
        Guid orderId,
        CreatePaymentSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await _paymentStore.GetPaymentTransactionByIdempotencyKeyAsync(
                request.IdempotencyKey.Trim(),
                cancellationToken);

            if (existing is not null)
            {
                return ApiResult<PaymentSessionResult>.Success(ToSessionResult(existing));
            }
        }

        var createResult = await _paymentStore.ExecuteInTransactionAsync(async ct =>
        {
            var order = await _paymentStore.GetOrderByIdAsync(orderId, ct);
            if (order is null)
            {
                return ApiResult<PaymentSessionResult>.Fail("Order not found.", 404);
            }

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                return ApiResult<PaymentSessionResult>.Fail("Order is already paid.", 409);
            }

            if (order.Status is OrderStatus.Cancelled or OrderStatus.Completed or OrderStatus.Failed or OrderStatus.ExecutionRejected or OrderStatus.RefundRequired)
            {
                return ApiResult<PaymentSessionResult>.Fail("Order cannot be paid in its current state.", 409);
            }

            if (order.TotalAmount <= 0)
            {
                return ApiResult<PaymentSessionResult>.Fail("Order amount must be greater than zero.", 400);
            }

            var paymentMethod = await EnsurePayOsPaymentMethodAsync(ct);
            if (!paymentMethod.IsActive)
            {
                return ApiResult<PaymentSessionResult>.Fail("PayOS payment method is inactive.", 409);
            }

            var now = DateTimeOffset.UtcNow;
            var paymentTransaction = new PaymentTransaction
            {
                OrderId = order.Id,
                PaymentMethodId = paymentMethod.Id,
                TransactionNumber = GenerateTransactionNumber(now),
                IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim(),
                CorrelationId = order.CorrelationId,
                Provider = _paymentGateway.ProviderCode,
                Amount = order.TotalAmount,
                Currency = order.Currency,
                Status = PaymentTransactionStatus.Pending,
                RequestedAt = now,
                RawRequestJson = JsonSerializer.Serialize(new
                {
                    orderId,
                    request.Description,
                    request.IdempotencyKey
                })
            };

            await _paymentStore.AddPaymentTransactionAsync(paymentTransaction, ct);
            await _paymentStore.SaveChangesAsync(ct);

            return ApiResult<PaymentSessionResult>.Success(ToSessionResult(paymentTransaction));
        }, cancellationToken);

        if (!createResult.Succeeded || createResult.Data is null)
        {
            return createResult;
        }

        var payment = await _paymentStore.GetPaymentTransactionByIdAsync(createResult.Data.PaymentTransactionId, cancellationToken);
        if (payment is null)
        {
            return ApiResult<PaymentSessionResult>.Fail("Payment transaction not found after creation.", 500);
        }

        try
        {
            var providerSession = await _paymentGateway.CreatePaymentSessionAsync(payment, payment.Order, cancellationToken);

            payment.ProviderOrderCode = providerSession.ProviderOrderCode;
            payment.ProviderPaymentLinkId = providerSession.ProviderPaymentLinkId;
            payment.ProviderTransactionId = providerSession.ProviderTransactionId;
            payment.CheckoutUrl = providerSession.CheckoutUrl;
            payment.QrCodePayload = providerSession.QrCodePayload;
            payment.ExpiresAt = providerSession.ExpiresAt;
            payment.ProviderStatus = providerSession.ProviderStatus;
            payment.RawResponseJson = providerSession.RawResponseJson;

            await _paymentStore.SaveChangesAsync(cancellationToken);

            return ApiResult<PaymentSessionResult>.Success(ToSessionResult(payment), "Payment session created.");
        }
        catch (Exception ex)
        {
            payment.MarkFailed("PROVIDER_SESSION_CREATE_FAILED", ex.Message, DateTimeOffset.UtcNow);
            await _paymentStore.SaveChangesAsync(cancellationToken);
            return ApiResult<PaymentSessionResult>.Fail("Failed to create provider payment session.", 502);
        }
    }

    public async Task<ApiResult<PaymentNotificationResult>> HandleProviderNotificationAsync(
        HandlePaymentProviderNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
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

            var alreadyProcessed = paymentTransaction.Status == PaymentTransactionStatus.Paid;
            if (!alreadyProcessed)
            {
                ApplyNotification(paymentTransaction, notification);
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

    public async Task<ApiResult<PaymentStatusResult>> GetOrderPaymentStatusAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var paymentTransaction = await _paymentStore.GetLatestPaymentTransactionByOrderIdAsync(orderId, cancellationToken);
        if (paymentTransaction is null)
        {
            return ApiResult<PaymentStatusResult>.Fail("Payment transaction not found.", 404);
        }

        return ApiResult<PaymentStatusResult>.Success(ToStatusResult(paymentTransaction));
    }

    public async Task<ApiResult<PaymentStatusResult>> GetPaymentTransactionStatusAsync(Guid paymentTransactionId, CancellationToken cancellationToken = default)
    {
        var paymentTransaction = await _paymentStore.GetPaymentTransactionByIdAsync(paymentTransactionId, cancellationToken);
        if (paymentTransaction is null)
        {
            return ApiResult<PaymentStatusResult>.Fail("Payment transaction not found.", 404);
        }

        return ApiResult<PaymentStatusResult>.Success(ToStatusResult(paymentTransaction));
    }

    private async Task<PaymentMethod> EnsurePayOsPaymentMethodAsync(CancellationToken cancellationToken)
    {
        var paymentMethod = await _paymentStore.GetPaymentMethodByCodeAsync(PayOsMethodCode, cancellationToken);
        if (paymentMethod is not null)
        {
            return paymentMethod;
        }

        paymentMethod = new PaymentMethod
        {
            Code = PayOsMethodCode,
            Name = "PayOS",
            Description = "PayOS payment gateway",
            Provider = _paymentGateway.ProviderCode,
            MethodType = "BankTransferQr",
            IsOnline = true,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _paymentStore.AddPaymentMethodAsync(paymentMethod, cancellationToken);
        await _paymentStore.SaveChangesAsync(cancellationToken);
        return paymentMethod;
    }

    private static void ApplyNotification(PaymentTransaction paymentTransaction, ProviderPaymentNotification notification)
    {
        paymentTransaction.ProviderPaymentLinkId = notification.ProviderPaymentLinkId ?? paymentTransaction.ProviderPaymentLinkId;
        paymentTransaction.ProviderTransactionId = notification.ProviderTransactionId ?? paymentTransaction.ProviderTransactionId;
        paymentTransaction.ProviderStatus = notification.ProviderStatus;
        paymentTransaction.PaidAmount = notification.PaidAmount ?? paymentTransaction.PaidAmount;
        paymentTransaction.ProviderPaidAt = notification.ProviderPaidAt ?? paymentTransaction.ProviderPaidAt;
        paymentTransaction.RawResponseJson = notification.RawPayloadJson;

        if (notification.IsPaid)
        {
            var paidAt = notification.ProviderPaidAt ?? DateTimeOffset.UtcNow;
            var paidAmount = notification.PaidAmount ?? paymentTransaction.Amount;
            paymentTransaction.MarkPaid(notification.ProviderTransactionId, paidAt, notification.RawPayloadJson);
            paymentTransaction.Order.MarkPaid(paidAmount, paidAt);
            return;
        }

        if (notification.IsCancelled)
        {
            paymentTransaction.Cancel(DateTimeOffset.UtcNow);
            paymentTransaction.Order.PaymentStatus = PaymentStatus.Cancelled;
            return;
        }

        if (notification.IsExpired)
        {
            paymentTransaction.Cancel(DateTimeOffset.UtcNow);
            paymentTransaction.Status = PaymentTransactionStatus.Expired;
            paymentTransaction.Order.PaymentStatus = PaymentStatus.Cancelled;
        }
    }

    private static PaymentSessionResult ToSessionResult(PaymentTransaction paymentTransaction)
    {
        return new PaymentSessionResult
        {
            PaymentTransactionId = paymentTransaction.Id,
            OrderId = paymentTransaction.OrderId,
            TransactionNumber = paymentTransaction.TransactionNumber,
            Provider = paymentTransaction.Provider,
            ProviderOrderCode = paymentTransaction.ProviderOrderCode,
            ProviderPaymentLinkId = paymentTransaction.ProviderPaymentLinkId,
            CheckoutUrl = paymentTransaction.CheckoutUrl,
            QrCodePayload = paymentTransaction.QrCodePayload,
            Amount = paymentTransaction.Amount,
            Currency = paymentTransaction.Currency,
            Status = paymentTransaction.Status,
            ExpiresAt = paymentTransaction.ExpiresAt
        };
    }

    private static PaymentStatusResult ToStatusResult(PaymentTransaction paymentTransaction)
    {
        return new PaymentStatusResult
        {
            PaymentTransactionId = paymentTransaction.Id,
            OrderId = paymentTransaction.OrderId,
            Provider = paymentTransaction.Provider,
            PaymentTransactionStatus = paymentTransaction.Status,
            OrderPaymentStatus = paymentTransaction.Order.PaymentStatus,
            OrderStatus = paymentTransaction.Order.Status,
            Amount = paymentTransaction.Amount,
            PaidAmount = paymentTransaction.PaidAmount,
            Currency = paymentTransaction.Currency,
            PaidAt = paymentTransaction.PaidAt,
            ExpiresAt = paymentTransaction.ExpiresAt
        };
    }

    private static string GenerateTransactionNumber(DateTimeOffset now)
    {
        return $"PAY-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..36].ToUpperInvariant();
    }
}
