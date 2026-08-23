using Application.Abstractions.Realtime;
using Application.Abstractions.Realtime.Events;
using Application.Payments.Abstractions;
using Application.Payments.PaymentSessions.Results;
using Application.Payments.PaymentSessions.Support;
using Application.Payments.PaymentSessions.Observability;
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
using Application.Orders.Admission;
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
            return ApiResult<PaymentNotificationResult>.ValidationFailure(new()
            {
                ["rawPayload"] = ["Webhook payload is required."]
            });
        }

        ProviderPaymentNotification notification;
        try
        {
            notification = await _paymentGateway.ParseAndVerifyNotificationAsync(
                request.RawPayload,
                request.Signature,
                cancellationToken);
        }
        catch (ProviderPaymentNotificationVerificationException exception)
        {
            return exception.Kind switch
            {
                ProviderPaymentNotificationVerificationFailureKind.InvalidPayload =>
                    ApiResult<PaymentNotificationResult>.BusinessFailure(PaymentErrors.WebhookPayloadInvalid),
                ProviderPaymentNotificationVerificationFailureKind.InvalidSignature =>
                    ApiResult<PaymentNotificationResult>.BusinessFailure(PaymentErrors.WebhookVerificationFailed),
                ProviderPaymentNotificationVerificationFailureKind.ConfigurationUnavailable =>
                    ApiResult<PaymentNotificationResult>.BusinessFailure(PaymentErrors.WebhookConfigurationUnavailable),
                _ => throw new ArgumentOutOfRangeException(nameof(exception.Kind), exception.Kind, "Unsupported webhook verification failure.")
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected payment webhook verification failure.");
            return ApiResult<PaymentNotificationResult>.Fail("Payment webhook processing failed.", 500);
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
        bool requiresMachineExecution = false;
        var verifiedUnmatched = false;
        PaymentWebhookAcknowledgementIssue? acknowledgementIssue = null;

        var result = await _paymentStore.ExecuteInTransactionAsync(async ct =>
        {
            if (!string.IsNullOrWhiteSpace(notification.ProviderEventId))
            {
                await _paymentStore.AcquirePaymentCallbackLockAsync(
                    notification.Provider,
                    notification.ProviderEventId,
                    ct);
            }

            if (!string.IsNullOrWhiteSpace(notification.ProviderOrderCode))
            {
                await _paymentStore.AcquireProviderPaymentLockAsync(
                    notification.Provider,
                    notification.ProviderOrderCode,
                    ct);
            }

            if (!string.IsNullOrWhiteSpace(notification.ProviderEventId))
            {
                var existingCallback = await _paymentStore.GetPaymentCallbackAsync(
                    notification.Provider,
                    notification.ProviderEventId,
                    ct);
                if (existingCallback is not null)
                {
                    var samePaymentIdentity = string.Equals(
                        existingCallback.PaymentTransaction.ProviderOrderCode,
                        notification.ProviderOrderCode,
                        StringComparison.Ordinal);
                    var samePayload = string.Equals(
                        existingCallback.PayloadJson,
                        notification.RawPayloadJson,
                        StringComparison.Ordinal);
                    if (!samePaymentIdentity || !samePayload)
                    {
                        acknowledgementIssue = PaymentWebhookAcknowledgementIssue.EventIdentityConflict;
                        return AcknowledgeExistingCallback(existingCallback);
                    }

                    if (existingCallback.ProcessingStatus != PaymentCallbackProcessingStatus.Processed)
                    {
                        acknowledgementIssue = PaymentWebhookAcknowledgementIssue.PreviouslyIgnored;
                        return AcknowledgeExistingCallback(existingCallback);
                    }

                    var existingPayment = existingCallback.PaymentTransaction;
                    return ApiResult<PaymentNotificationResult>.Success(new PaymentNotificationResult
                    {
                        PaymentTransactionId = existingPayment.Id,
                        OrderId = existingPayment.OrderId,
                        Status = existingPayment.Status,
                        AlreadyProcessed = true
                    });
                }
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
                verifiedUnmatched = true;
                var acknowledgement = ApiResult<PaymentNotificationResult>.Success();
                acknowledgement.Message = "Verified payment callback acknowledged without a matching local transaction.";
                return acknowledgement;
            }

            await _paymentStore.AcquireOrderWorkflowLockAsync(paymentTransaction.OrderId, ct);
            await _paymentStore.ReloadOrderAsync(paymentTransaction.Order, ct);
            await _paymentStore.AcquireKioskOperationalLockAsync(paymentTransaction.Order.KioskId, ct);

            var appliedSettlement = notification.IsPaid
                ? await _paymentStore.GetAppliedPaymentSettlementByOrderIdAsync(paymentTransaction.OrderId, ct)
                : null;
            var paidByDifferentTransaction = appliedSettlement is not null &&
                appliedSettlement.Id != paymentTransaction.Id;
            var paymentConflictsWithActiveCustomerSession = notification.IsPaid &&
                await _paymentStore.HasActiveCustomerSessionAsync(
                    paymentTransaction.Order.KioskId,
                    DateTimeOffset.UtcNow,
                    paymentTransaction.OrderId,
                    ct);

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

            var validationError = PaymentNotificationApplier.ValidateNotification(paymentTransaction, notification);
            if (validationError is not null)
            {
                callback.MarkIgnored(PaymentNotificationApplier.ToDiagnosticCode(validationError.Value), DateTimeOffset.UtcNow);
                await _paymentStore.AddPaymentCallbackAsync(callback, ct);
                await _paymentStore.SaveChangesAsync(ct);
                acknowledgementIssue = PaymentWebhookAcknowledgementIssue.InvalidSettlement;
                return ApiResult<PaymentNotificationResult>.Success(new PaymentNotificationResult
                {
                    PaymentTransactionId = paymentTransaction.Id,
                    OrderId = paymentTransaction.OrderId,
                    Status = paymentTransaction.Status,
                    AlreadyProcessed = false
                }, "Payment callback acknowledged.");
            }

            await _paymentStore.AddPaymentCallbackAsync(callback, ct);

            originalStatus = paymentTransaction.Order.Status;
            originalTxStatus = paymentTransaction.Status;
            var alreadyProcessed = paymentTransaction.Status == PaymentTransactionStatus.Paid;
            if (!alreadyProcessed)
            {
                PaymentNotificationApplier.ApplyNotification(paymentTransaction, notification);
                if (notification.IsPaid && paymentTransaction.Status == PaymentTransactionStatus.Paid)
                {
                    var paidAt = notification.ProviderPaidAt ?? DateTimeOffset.UtcNow;
                    var paidAmount = notification.PaidAmount ?? paymentTransaction.Amount;
                    if (paidByDifferentTransaction)
                    {
                        appliedSettlement!.AssignPrimarySettlement();
                        const string duplicateReason =
                            "A second provider-confirmed payment was received for an already settled order. Manual refund review is required.";
                        paymentTransaction.MarkDuplicateRefundRequired(duplicateReason);
                        paymentTransaction.Order.MarkDuplicatePaymentRefundRequired(duplicateReason);
                    }
                    else
                    {
                        paymentTransaction.AssignPrimarySettlement();
                        paymentTransaction.Order.MarkPaid(paidAmount, paidAt);
                        if (paymentConflictsWithActiveCustomerSession)
                        {
                            paymentTransaction.Order.MarkRefundRequired(
                                "Payment was confirmed after another customer session became active at this kiosk. Manual refund review is required.");
                        }
                    }
                }

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
                requiresMachineExecution = paymentTransaction.Order.Status == OrderStatus.ReadyForFulfillment &&
                    paymentTransaction.Order.OrderItems.Any(item =>
                        item.FulfillmentType == Domain.Catalog.Enums.FulfillmentType.MachineProduced);

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

        if (verifiedUnmatched)
        {
            PaymentWebhookMetrics.RecordVerifiedUnmatched();
            _logger.LogInformation(
                "Verified payment callback was acknowledged without a matching local transaction. Provider={Provider}, EventType={EventType}",
                notification.Provider,
                notification.EventType);
            return result;
        }

        if (acknowledgementIssue is not null)
        {
            if (acknowledgementIssue == PaymentWebhookAcknowledgementIssue.EventIdentityConflict)
            {
                PaymentWebhookMetrics.RecordVerifiedEventConflict();
            }

            _logger.LogWarning(
                "Verified payment callback acknowledged without applying a settlement. Issue={Issue}, Provider={Provider}, EventId={EventId}",
                acknowledgementIssue,
                notification.Provider,
                notification.ProviderEventId);
            return result;
        }

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

            if (result.Data.Status == PaymentTransactionStatus.Paid && requiresMachineExecution)
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

    private static ApiResult<PaymentNotificationResult> AcknowledgeExistingCallback(PaymentCallback callback) =>
        ApiResult<PaymentNotificationResult>.Success(new PaymentNotificationResult
        {
            PaymentTransactionId = callback.PaymentTransactionId,
            OrderId = callback.PaymentTransaction.OrderId,
            Status = callback.PaymentTransaction.Status,
            AlreadyProcessed = true
        }, "Payment callback acknowledged.");

    private enum PaymentWebhookAcknowledgementIssue
    {
        EventIdentityConflict,
        PreviouslyIgnored,
        InvalidSettlement
    }
}
