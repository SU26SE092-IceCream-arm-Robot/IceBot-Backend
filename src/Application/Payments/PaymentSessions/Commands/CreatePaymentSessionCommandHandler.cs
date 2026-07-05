using Application.Payments.Abstractions;
using Application.Payments.PaymentSessions.Mapping;
using Application.Payments.PaymentSessions.Results;
using Application.Payments.PaymentSessions.Support;
using Application.Shared.Wrappers;
using Application.Tenants.Kiosks.Rules;
using Domain.Orders.Enums;
using Domain.Payments.Entities;
using Domain.Payments.Enums;
using System.Text.Json;

namespace Application.Payments.PaymentSessions.Commands;

public sealed class CreatePaymentSessionCommandHandler
{
    private readonly IPaymentStore _paymentStore;
    private readonly IPaymentGateway _paymentGateway;

    public CreatePaymentSessionCommandHandler(IPaymentStore paymentStore, IPaymentGateway paymentGateway)
    {
        _paymentStore = paymentStore;
        _paymentGateway = paymentGateway;
    }

    public async Task<ApiResult<PaymentSessionResult>> HandleAsync(
        CreatePaymentSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        var orderId = command.OrderId;
        var request = command.Request;
        var idempotencyKey = string.IsNullOrWhiteSpace(command.IdempotencyKey)
            ? null
            : command.IdempotencyKey.Trim();
        var paymentMethodCode = request.PaymentMethodCode.Trim().ToLowerInvariant();
        var expectedCurrency = request.ExpectedCurrency.Trim().ToUpperInvariant();

        if (!string.Equals(paymentMethodCode, PayOsPaymentMethodResolver.MethodCode, StringComparison.Ordinal))
        {
            return ApiResult<PaymentSessionResult>.Fail("Payment method is not supported.", 400);
        }

        if (idempotencyKey is not null)
        {
            var existing = await _paymentStore.GetPaymentTransactionByIdempotencyKeyAsync(
                idempotencyKey,
                cancellationToken);

            if (existing is not null)
            {
                if (existing.OrderId != orderId ||
                    !string.Equals(existing.PaymentMethod.Code, paymentMethodCode, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(existing.Provider, _paymentGateway.ProviderCode, StringComparison.OrdinalIgnoreCase) ||
                    existing.Amount != request.ExpectedAmount ||
                    !string.Equals(existing.Currency, expectedCurrency, StringComparison.OrdinalIgnoreCase))
                {
                    return ApiResult<PaymentSessionResult>.Fail(
                        "Idempotency key was already used for a different payment request.",
                        409);
                }

                return ApiResult<PaymentSessionResult>.Success(PaymentSessionResultMapper.ToSessionResult(existing));
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

            var salesAvailabilityError = KioskSalesAvailabilityRules.ValidateOnlineSalesAvailability(order.Kiosk);
            if (salesAvailabilityError is not null)
            {
                return ApiResult<PaymentSessionResult>.Fail(salesAvailabilityError, 409);
            }

            if (order.Status is OrderStatus.Cancelled or OrderStatus.Completed or OrderStatus.Failed or OrderStatus.ExecutionRejected or OrderStatus.RefundRequired)
            {
                return ApiResult<PaymentSessionResult>.Fail("Order cannot be paid in its current state.", 409);
            }

            if (order.TotalAmount <= 0)
            {
                return ApiResult<PaymentSessionResult>.Fail("Order amount must be greater than zero.", 400);
            }

            if (request.ExpectedAmount != order.TotalAmount ||
                !string.Equals(expectedCurrency, order.Currency, StringComparison.OrdinalIgnoreCase))
            {
                return ApiResult<PaymentSessionResult>.Fail(
                        "Displayed payment amount no longer matches the order total.",
                        409)
                    .AddDetail("expectedAmount", request.ExpectedAmount)
                    .AddDetail("orderAmount", order.TotalAmount)
                    .AddDetail("expectedCurrency", expectedCurrency)
                    .AddDetail("orderCurrency", order.Currency);
            }

            var paymentMethod = await PayOsPaymentMethodResolver.EnsurePayOsPaymentMethodAsync(_paymentStore, _paymentGateway.ProviderCode, ct);
            if (!paymentMethod.IsActive)
            {
                return ApiResult<PaymentSessionResult>.Fail("PayOS payment method is inactive.", 409);
            }

            var now = DateTimeOffset.UtcNow;
            var paymentTransaction = new PaymentTransaction
            {
                OrderId = order.Id,
                PaymentMethodId = paymentMethod.Id,
                TransactionNumber = PaymentTransactionNumberGenerator.GenerateTransactionNumber(now),
                IdempotencyKey = idempotencyKey,
                CorrelationId = order.CorrelationId,
                Provider = _paymentGateway.ProviderCode,
                Amount = order.TotalAmount,
                Currency = order.Currency,
                Status = PaymentTransactionStatus.Pending,
                RequestedAt = now,
                RawRequestJson = JsonSerializer.Serialize(new
                {
                    orderId,
                    orderNumber = order.OrderNumber,
                    paymentMethodCode,
                    idempotencyKey
                })
            };

            await _paymentStore.AddPaymentTransactionAsync(paymentTransaction, ct);
            await _paymentStore.SaveChangesAsync(ct);

            return ApiResult<PaymentSessionResult>.Success(PaymentSessionResultMapper.ToSessionResult(paymentTransaction));
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

            return ApiResult<PaymentSessionResult>.Success(PaymentSessionResultMapper.ToSessionResult(payment), "Payment session created.");
        }
        catch (Exception ex)
        {
            payment.MarkFailed("PROVIDER_SESSION_CREATE_FAILED", ex.Message, DateTimeOffset.UtcNow);
            await _paymentStore.SaveChangesAsync(cancellationToken);
            return ApiResult<PaymentSessionResult>.Fail("Failed to create provider payment session.", 502);
        }
    }
}
