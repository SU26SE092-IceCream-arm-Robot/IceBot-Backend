using Application.Payments.Abstractions;
using Application.Payments.PaymentSessions.Mapping;
using Application.Payments.PaymentSessions.Results;
using Application.Payments.PaymentSessions.Support;
using Application.Payments.Providers;
using Application.Shared.Wrappers;
using Application.Shared.Idempotency;
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
        if (!ScopedIdempotencyKey.TryNormalize(command.IdempotencyKey, out var idempotencyKey))
        {
            return ApiResult<PaymentSessionResult>.Fail(
                $"Idempotency-Key is required and must be at most {ScopedIdempotencyKey.MaxClientKeyLength} characters.",
                400);
        }
        var scopedIdempotencyKey = ScopedIdempotencyKey.ForOrder(orderId, idempotencyKey);
        var paymentMethodCode = request.PaymentMethodCode.Trim().ToLowerInvariant();
        var expectedCurrency = request.ExpectedCurrency.Trim().ToUpperInvariant();

        if (!string.Equals(paymentMethodCode, PayOsPaymentMethodResolver.MethodCode, StringComparison.Ordinal))
        {
            return ApiResult<PaymentSessionResult>.Fail("Payment method is not supported.", 400);
        }

        var createdNewTransaction = false;

        var createResult = await _paymentStore.ExecuteInTransactionAsync(async ct =>
        {
            await _paymentStore.AcquirePaymentSessionLockAsync(orderId, ct);
            await _paymentStore.AcquireOrderWorkflowLockAsync(orderId, ct);

            var order = await _paymentStore.GetOrderByIdAsync(orderId, ct);
            if (order is null)
            {
                return ApiResult<PaymentSessionResult>.Fail("Order not found.", 404);
            }

            var existingByIdempotencyKey = await _paymentStore.GetPaymentTransactionByIdempotencyKeyAsync(
                scopedIdempotencyKey,
                ct);
            if (existingByIdempotencyKey is not null)
            {
                if (!MatchesRequest(existingByIdempotencyKey, orderId, paymentMethodCode, expectedCurrency, request.ExpectedAmount))
                {
                    return ApiResult<PaymentSessionResult>.Fail(
                        "Idempotency key was already used for a different payment request.",
                        409);
                }

                if (existingByIdempotencyKey.Status == PaymentTransactionStatus.Failed &&
                    !HasPaymentInstructions(existingByIdempotencyKey))
                {
                    return ApiResult<PaymentSessionResult>.Fail(
                        "The previous payment session creation failed. Retry with a new idempotency key.",
                        409);
                }

                return !HasPaymentInstructions(existingByIdempotencyKey) &&
                    existingByIdempotencyKey.Status is PaymentTransactionStatus.Pending or PaymentTransactionStatus.Authorized
                    ? ApiResult<PaymentSessionResult>.Fail("Payment session creation is in progress. Retry with the same idempotency key.", 409)
                    : ApiResult<PaymentSessionResult>.Success(PaymentSessionResultMapper.ToSessionResult(existingByIdempotencyKey));
            }

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                return ApiResult<PaymentSessionResult>.Fail("Order is already paid.", 409);
            }

            var connectivity = await _paymentStore.GetKioskConnectivityAsync(order.KioskId, ct);
            var salesAvailabilityError = KioskSalesAvailabilityRules.ValidateOnlineSalesAvailability(order.Kiosk, connectivity);
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

            var activeSession = await _paymentStore.GetActivePaymentTransactionByOrderIdAsync(order.Id, ct);
            if (activeSession is not null)
            {
                return !HasPaymentInstructions(activeSession)
                    ? ApiResult<PaymentSessionResult>.Fail("Payment session creation is in progress. Retry shortly.", 409)
                    : ApiResult<PaymentSessionResult>.Success(
                        PaymentSessionResultMapper.ToSessionResult(activeSession),
                        "Existing active payment session returned.");
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
                IdempotencyKey = scopedIdempotencyKey,
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
            paymentTransaction.ProviderOrderCode = _paymentGateway.CreateProviderOrderCode(paymentTransaction.Id);

            await _paymentStore.AddPaymentTransactionAsync(paymentTransaction, ct);
            await _paymentStore.SaveChangesAsync(ct);
            createdNewTransaction = true;

            return ApiResult<PaymentSessionResult>.Success(PaymentSessionResultMapper.ToSessionResult(paymentTransaction));
        }, cancellationToken);

        if (!createResult.Succeeded || createResult.Data is null)
        {
            return createResult;
        }

        if (!createdNewTransaction)
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ProviderPaymentSessionCreationException ex)
        {
            var now = DateTimeOffset.UtcNow;
            if (ex.OutcomeUnknown)
            {
                payment.MarkAttempted(now);
                payment.ScheduleRetry(
                    "PROVIDER_SESSION_CREATE_OUTCOME_UNKNOWN",
                    ex.Message,
                    now.AddSeconds(30));
            }
            else
            {
                payment.MarkFailed("PROVIDER_SESSION_CREATE_REJECTED", ex.Message, now);
            }

            await _paymentStore.SaveChangesAsync(cancellationToken);
            return ApiResult<PaymentSessionResult>.Fail("Failed to create provider payment session.", 502);
        }
        catch (Exception ex)
        {
            payment.MarkFailed("PROVIDER_SESSION_CREATE_FAILED", ex.Message, DateTimeOffset.UtcNow);
            await _paymentStore.SaveChangesAsync(cancellationToken);
            return ApiResult<PaymentSessionResult>.Fail("Failed to create provider payment session.", 502);
        }
    }

    private bool MatchesRequest(
        PaymentTransaction transaction,
        Guid orderId,
        string paymentMethodCode,
        string expectedCurrency,
        decimal expectedAmount) =>
        transaction.OrderId == orderId &&
        transaction.PaymentMethod is not null &&
        string.Equals(transaction.PaymentMethod.Code, paymentMethodCode, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(transaction.Provider, _paymentGateway.ProviderCode, StringComparison.OrdinalIgnoreCase) &&
        transaction.Amount == expectedAmount &&
        string.Equals(transaction.Currency, expectedCurrency, StringComparison.OrdinalIgnoreCase);

    private static bool HasPaymentInstructions(PaymentTransaction transaction) =>
        !string.IsNullOrWhiteSpace(transaction.CheckoutUrl) ||
        !string.IsNullOrWhiteSpace(transaction.QrCodePayload);
}
