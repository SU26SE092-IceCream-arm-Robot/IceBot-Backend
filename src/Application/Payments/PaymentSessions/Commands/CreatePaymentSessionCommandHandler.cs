using Application.Payments.Abstractions;
using Application.Payments.PaymentSessions;
using Application.Payments.PaymentSessions.Mapping;
using Application.Payments.PaymentSessions.Requests;
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
        var validationErrors = ValidateRequest(request);
        if (validationErrors is not null)
        {
            return ApiResult<PaymentSessionResult>.ValidationFailure(validationErrors);
        }

        if (!ScopedIdempotencyKey.TryNormalize(command.IdempotencyKey, out var idempotencyKey))
        {
            return ApiResult<PaymentSessionResult>.BusinessFailure(
                PaymentErrors.IdempotencyKeyInvalid);
        }
        var scopedIdempotencyKey = ScopedIdempotencyKey.ForOrder(orderId, idempotencyKey);
        var paymentMethodCode = request.PaymentMethodCode.Trim().ToLowerInvariant();
        var expectedCurrency = request.ExpectedCurrency.Trim().ToUpperInvariant();
        var isCashPayment = CashPaymentMethodResolver.IsCash(paymentMethodCode);
        var isPayOsPayment = string.Equals(paymentMethodCode, PayOsPaymentMethodResolver.MethodCode, StringComparison.Ordinal);
        var providerCode = isCashPayment ? CashPaymentMethodResolver.ProviderCode : _paymentGateway.ProviderCode;

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
                    return ApiResult<PaymentSessionResult>.BusinessFailure(
                        PaymentErrors.IdempotencyConflict);
                }

                if (HasExpiredOrderPaymentWindow(order, DateTimeOffset.UtcNow))
                {
                    return ApiResult<PaymentSessionResult>.BusinessFailure(
                        PaymentErrors.WindowExpired);
                }

                if (existingByIdempotencyKey.Status == PaymentTransactionStatus.Failed &&
                    !HasPaymentInstructions(existingByIdempotencyKey))
                {
                    return ApiResult<PaymentSessionResult>.BusinessFailure(
                        PaymentErrors.PreviousSessionFailed);
                }

                return isCashPayment && IsPendingCashConfirmation(existingByIdempotencyKey)
                    ? ApiResult<PaymentSessionResult>.Success(
                        PaymentSessionResultMapper.ToSessionResult(existingByIdempotencyKey),
                        "Cash payment is awaiting staff confirmation.")
                    : !HasPaymentInstructions(existingByIdempotencyKey) &&
                    existingByIdempotencyKey.Status is PaymentTransactionStatus.Pending or PaymentTransactionStatus.Authorized
                    ? ApiResult<PaymentSessionResult>.BusinessFailure(
                        PaymentErrors.SessionCreationInProgress)
                    : ApiResult<PaymentSessionResult>.Success(PaymentSessionResultMapper.ToSessionResult(existingByIdempotencyKey));
            }

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                return ApiResult<PaymentSessionResult>.BusinessFailure(
                    PaymentErrors.OrderAlreadyPaid);
            }

            var admission = await _admissionEvaluator.EvaluateAsync(
                order.Kiosk,
                new KioskSalesAdmissionRequest(DateTimeOffset.UtcNow, order.Id),
                ct);
            if (!admission.CanOpenPayment)
            {
                var blocker = admission.PrimaryBlocker
                    ?? throw new InvalidOperationException("Blocked kiosk admission must provide a blocker.");
                return ApiResult<PaymentSessionResult>.BusinessFailure(
                    SalesAdmissionErrors.For(blocker.Code));
            }

            var sellabilityFailure = await _sellabilityGuard.ValidateAsync(order, DateTimeOffset.UtcNow, ct);
            if (sellabilityFailure is not null)
            {
                return ApiResult<PaymentSessionResult>.BusinessFailure(
                    SalesAdmissionErrors.For(sellabilityFailure.Blocker.Code));
            }

            if (order.Status is OrderStatus.Cancelled or OrderStatus.Completed or OrderStatus.Failed or OrderStatus.ExecutionRejected or OrderStatus.RefundRequired)
            {
                return ApiResult<PaymentSessionResult>.BusinessFailure(
                    PaymentErrors.OrderNotPayable);
            }

            if (order.TotalAmount <= 0)
            {
                return ApiResult<PaymentSessionResult>.Fail("Order cannot be paid because its total is invalid.", 500);
            }

            var now = DateTimeOffset.UtcNow;
            if (HasExpiredOrderPaymentWindow(order, now))
            {
                return ApiResult<PaymentSessionResult>.BusinessFailure(
                    PaymentErrors.WindowExpired);
            }

            var activeSession = await _paymentStore.GetActivePaymentTransactionByOrderIdAsync(order.Id, ct);
            if (activeSession is not null)
            {
                return isCashPayment && IsPendingCashConfirmation(activeSession)
                    ? ApiResult<PaymentSessionResult>.Success(
                        PaymentSessionResultMapper.ToSessionResult(activeSession),
                        "Existing cash payment is awaiting staff confirmation.")
                    : !HasPaymentInstructions(activeSession)
                    ? ApiResult<PaymentSessionResult>.BusinessFailure(
                        PaymentErrors.SessionCreationInProgress)
                    : ApiResult<PaymentSessionResult>.Success(
                        PaymentSessionResultMapper.ToSessionResult(activeSession),
                        "Existing active payment session returned.");
            }

            if (request.ExpectedAmount != order.TotalAmount ||
                !string.Equals(expectedCurrency, order.Currency, StringComparison.OrdinalIgnoreCase))
            {
                return ApiResult<PaymentSessionResult>.BusinessFailure(
                        PaymentErrors.AmountChanged)
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
                return ApiResult<PaymentSessionResult>.BusinessFailure(
                    PaymentErrors.MethodNotConfigured);
            }
            if (!paymentMethod.IsActive)
            {
                return ApiResult<PaymentSessionResult>.BusinessFailure(
                    PaymentErrors.MethodInactive);
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
            return ApiResult<PaymentSessionResult>.BusinessFailure(
                PaymentErrors.WindowExpired);
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
            switch (ex.FailureKind)
            {
                case ProviderPaymentSessionFailureKind.OutcomeUnknown:
                    payment.MarkAttempted(now);
                    payment.ScheduleRetry(
                        "PROVIDER_SESSION_CREATE_OUTCOME_UNKNOWN",
                        ex.Message,
                        now.AddSeconds(30));
                    await _paymentStore.SaveChangesAsync(cancellationToken);
                    return ApiResult<PaymentSessionResult>.BusinessFailure(
                        PaymentErrors.ProviderOutcomeUnknown);

                case ProviderPaymentSessionFailureKind.Unavailable:
                    payment.MarkAttempted(now);
                    payment.ScheduleRetry(
                        "PROVIDER_SESSION_CREATE_UNAVAILABLE",
                        ex.Message,
                        now.AddSeconds(30));
                    await _paymentStore.SaveChangesAsync(cancellationToken);
                    return ApiResult<PaymentSessionResult>.BusinessFailure(
                        PaymentErrors.ProviderUnavailable);

                case ProviderPaymentSessionFailureKind.Rejected:
                    payment.MarkFailed("PROVIDER_SESSION_CREATE_REJECTED", ex.Message, now);
                    await _paymentStore.SaveChangesAsync(cancellationToken);
                    return ApiResult<PaymentSessionResult>.BusinessFailure(
                        PaymentErrors.ProviderRejected);

                default:
                    throw new ArgumentOutOfRangeException(nameof(ex.FailureKind), ex.FailureKind, "Unsupported provider failure kind.");
            }
        }
        catch (Exception ex)
        {
            var now = DateTimeOffset.UtcNow;
            payment.MarkAttempted(now);
            payment.ScheduleRetry(
                "PROVIDER_SESSION_CREATE_UNEXPECTED_FAILURE",
                ex.Message,
                now.AddSeconds(30));
            await _paymentStore.SaveChangesAsync(cancellationToken);
            throw;
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

    private static Dictionary<string, List<string>>? ValidateRequest(CreatePaymentSessionRequest request)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(request.PaymentMethodCode))
        {
            AddValidationError(errors, "paymentMethodCode", "Payment method is required.");
        }
        else if (request.PaymentMethodCode.Trim().Length > 50 ||
                 (!CashPaymentMethodResolver.IsCash(request.PaymentMethodCode.Trim()) &&
                  !string.Equals(
                      request.PaymentMethodCode.Trim(),
                      PayOsPaymentMethodResolver.MethodCode,
                      StringComparison.OrdinalIgnoreCase)))
        {
            AddValidationError(errors, "paymentMethodCode", "Payment method is not supported.");
        }

        if (request.ExpectedAmount <= 0)
            AddValidationError(errors, "expectedAmount", "Expected amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(request.ExpectedCurrency) || request.ExpectedCurrency.Trim().Length != 3)
            AddValidationError(errors, "expectedCurrency", "Expected currency must be a three-letter code.");

        return errors.Count == 0 ? null : errors;
    }

    private static void AddValidationError(
        Dictionary<string, List<string>> errors,
        string field,
        string message)
    {
        if (!errors.TryGetValue(field, out var fieldErrors))
        {
            fieldErrors = [];
            errors[field] = fieldErrors;
        }

        fieldErrors.Add(message);
    }
}
