using Application.Payments.Abstractions;
using Application.Payments.PaymentSessions.Mapping;
using Application.Payments.PaymentSessions.Results;
using Application.Payments.PaymentSessions.Support;
using Application.Payments.Providers;
using Application.Shared.Wrappers;
using Application.Shared.Idempotency;
using Application.Orders.Admission;
using Application.SalesCatalog.Admission;
using Application.SalesCatalog.Admission.Services;
using Domain.Orders.Enums;
using Domain.Orders.Entities;
using Domain.Payments.Entities;
using Domain.Payments.Enums;
using System.Text.Json;

namespace Application.Payments.PaymentSessions.Commands;

public sealed class CreatePaymentSessionCommandHandler
{
    private readonly IPaymentStore _paymentStore;
    private readonly IPaymentGateway _paymentGateway;

    private readonly OrderPaymentSellabilityGuard _sellabilityGuard;
    private readonly KioskSalesAdmissionEvaluator _admissionEvaluator;

    public CreatePaymentSessionCommandHandler(
        IPaymentStore paymentStore,
        IPaymentGateway paymentGateway,
        OrderPaymentSellabilityGuard sellabilityGuard,
        KioskSalesAdmissionEvaluator admissionEvaluator)
    {
        _paymentStore = paymentStore;
        _paymentGateway = paymentGateway;
        _sellabilityGuard = sellabilityGuard;
        _admissionEvaluator = admissionEvaluator;
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
        var isCashPayment = CashPaymentMethodResolver.IsCash(paymentMethodCode);
        var isPayOsPayment = string.Equals(paymentMethodCode, PayOsPaymentMethodResolver.MethodCode, StringComparison.Ordinal);
        var providerCode = isCashPayment ? CashPaymentMethodResolver.ProviderCode : _paymentGateway.ProviderCode;

        if (!isCashPayment && !isPayOsPayment)
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

            await _paymentStore.AcquireKioskOperationalLockAsync(order.KioskId, ct);

            var existingByIdempotencyKey = await _paymentStore.GetPaymentTransactionByIdempotencyKeyAsync(
                scopedIdempotencyKey,
                ct);
            if (existingByIdempotencyKey is not null)
            {
                if (!MatchesRequest(existingByIdempotencyKey, orderId, paymentMethodCode, providerCode, expectedCurrency, request.ExpectedAmount))
                {
                    return ApiResult<PaymentSessionResult>.Fail(
                        "Idempotency key was already used for a different payment request.",
                        409);
                }

                if (HasExpiredOrderPaymentWindow(order, DateTimeOffset.UtcNow))
                {
                    return ApiResult<PaymentSessionResult>.Fail(
                        "The order payment window has expired.",
                        409);
                }

                if (existingByIdempotencyKey.Status == PaymentTransactionStatus.Failed &&
                    !HasPaymentInstructions(existingByIdempotencyKey))
                {
                    return ApiResult<PaymentSessionResult>.Fail(
                        "The previous payment session creation failed. Retry with a new idempotency key.",
                        409);
                }

                return isCashPayment && IsPendingCashConfirmation(existingByIdempotencyKey)
                    ? ApiResult<PaymentSessionResult>.Success(
                        PaymentSessionResultMapper.ToSessionResult(existingByIdempotencyKey),
                        "Cash payment is awaiting staff confirmation.")
                    : !HasPaymentInstructions(existingByIdempotencyKey) &&
                    existingByIdempotencyKey.Status is PaymentTransactionStatus.Pending or PaymentTransactionStatus.Authorized
                    ? ApiResult<PaymentSessionResult>.Fail("Payment session creation is in progress. Retry with the same idempotency key.", 409)
                    : ApiResult<PaymentSessionResult>.Success(PaymentSessionResultMapper.ToSessionResult(existingByIdempotencyKey));
            }

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                return ApiResult<PaymentSessionResult>.Fail("Order is already paid.", 409);
            }

            var admission = await _admissionEvaluator.EvaluateAsync(
                order.Kiosk,
                new KioskSalesAdmissionRequest(DateTimeOffset.UtcNow, order.Id),
                ct);
            if (!admission.CanOpenPayment)
            {
                return ApiResult<PaymentSessionResult>.Fail(admission.ToDisplayMessage()!, 409);
            }

            var sellabilityError = await _sellabilityGuard.ValidateAsync(order, DateTimeOffset.UtcNow, ct);
            if (sellabilityError is not null)
            {
                return ApiResult<PaymentSessionResult>.Fail(sellabilityError, 409);
            }

            if (order.Status is OrderStatus.Cancelled or OrderStatus.Completed or OrderStatus.Failed or OrderStatus.ExecutionRejected or OrderStatus.RefundRequired)
            {
                return ApiResult<PaymentSessionResult>.Fail("Order cannot be paid in its current state.", 409);
            }

            if (order.TotalAmount <= 0)
            {
                return ApiResult<PaymentSessionResult>.Fail("Order amount must be greater than zero.", 400);
            }

            var now = DateTimeOffset.UtcNow;
            if (HasExpiredOrderPaymentWindow(order, now))
            {
                return ApiResult<PaymentSessionResult>.Fail(
                    "The order payment window has expired.",
                    409);
            }

            var activeSession = await _paymentStore.GetActivePaymentTransactionByOrderIdAsync(order.Id, ct);
            if (activeSession is not null)
            {
                return isCashPayment && IsPendingCashConfirmation(activeSession)
                    ? ApiResult<PaymentSessionResult>.Success(
                        PaymentSessionResultMapper.ToSessionResult(activeSession),
                        "Existing cash payment is awaiting staff confirmation.")
                    : !HasPaymentInstructions(activeSession)
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

            var paymentMethod = isCashPayment
                ? await CashPaymentMethodResolver.GetCashPaymentMethodAsync(_paymentStore, ct)
                : await PayOsPaymentMethodResolver.EnsurePayOsPaymentMethodAsync(_paymentStore, _paymentGateway.ProviderCode, ct);
            if (paymentMethod is null)
            {
                return ApiResult<PaymentSessionResult>.Fail("Cash payment method is not configured.", 409);
            }
            if (!paymentMethod.IsActive)
            {
                return ApiResult<PaymentSessionResult>.Fail(
                    isCashPayment ? "Cash payment method is inactive." : "PayOS payment method is inactive.",
                    409);
            }

            var paymentTransaction = new PaymentTransaction
            {
                OrderId = order.Id,
                PaymentMethodId = paymentMethod.Id,
                TransactionNumber = PaymentTransactionNumberGenerator.GenerateTransactionNumber(now),
                IdempotencyKey = scopedIdempotencyKey,
                CorrelationId = order.CorrelationId,
                Provider = providerCode,
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
            if (!isCashPayment)
            {
                paymentTransaction.ProviderOrderCode = _paymentGateway.CreateProviderOrderCode(paymentTransaction.Id);
            }

            await _paymentStore.AddPaymentTransactionAsync(paymentTransaction, ct);
            await _paymentStore.SaveChangesAsync(ct);
            createdNewTransaction = true;

            return ApiResult<PaymentSessionResult>.Success(PaymentSessionResultMapper.ToSessionResult(paymentTransaction));
        }, cancellationToken);

        if (!createResult.Succeeded || createResult.Data is null)
        {
            return createResult;
        }

        if (!createdNewTransaction || isCashPayment)
        {
            return isCashPayment && createResult.Succeeded
                ? ApiResult<PaymentSessionResult>.Success(createResult.Data!, "Cash payment is awaiting staff confirmation.")
                : createResult;
        }

        var payment = await _paymentStore.GetPaymentTransactionByIdAsync(createResult.Data.PaymentTransactionId, cancellationToken);
        if (payment is null)
        {
            return ApiResult<PaymentSessionResult>.Fail("Payment transaction not found after creation.", 500);
        }

        var providerRequestStartedAt = DateTimeOffset.UtcNow;
        if (HasExpiredOrderPaymentWindow(payment.Order, providerRequestStartedAt))
        {
            payment.MarkExpired(providerRequestStartedAt);
            payment.ExpiresAt = payment.Order.PaymentDeadlineAt;
            await _paymentStore.SaveChangesAsync(cancellationToken);
            return ApiResult<PaymentSessionResult>.Fail("The order payment window has expired.", 409);
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
        string providerCode,
        string expectedCurrency,
        decimal expectedAmount) =>
        transaction.OrderId == orderId &&
        transaction.PaymentMethod is not null &&
        string.Equals(transaction.PaymentMethod.Code, paymentMethodCode, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(transaction.Provider, providerCode, StringComparison.OrdinalIgnoreCase) &&
        transaction.Amount == expectedAmount &&
        string.Equals(transaction.Currency, expectedCurrency, StringComparison.OrdinalIgnoreCase);

    private static bool HasPaymentInstructions(PaymentTransaction transaction) =>
        !string.IsNullOrWhiteSpace(transaction.CheckoutUrl) ||
        !string.IsNullOrWhiteSpace(transaction.QrCodePayload);

    private static bool IsPendingCashConfirmation(PaymentTransaction transaction) =>
        CashPaymentMethodResolver.IsCash(transaction.PaymentMethod?.Code) &&
        transaction.Status is PaymentTransactionStatus.Pending or PaymentTransactionStatus.Authorized;

    private static bool HasExpiredOrderPaymentWindow(Order order, DateTimeOffset observedAt) =>
        order.PaymentDeadlineAt != default && order.PaymentDeadlineAt <= observedAt;
}
