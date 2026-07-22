using Application.Payments.Abstractions;
using Application.Payments.Providers;
using Application.Payments.PaymentSessions.Notifications;
using Application.Payments.PaymentSessions.Support;
using Domain.Payments.Entities;
using Domain.Payments.Enums;
using Domain.Orders.Enums;

namespace Application.Payments.PaymentSessions.Commands;

public sealed class ReconcilePendingPaymentSessionCommandHandler(
    IPaymentStore paymentStore,
    IPaymentGateway paymentGateway,
    IPaymentInterventionNotifier interventionNotifier)
{
    public async Task<PaymentSessionReconciliationOutcome> HandleAsync(
        ReconcilePendingPaymentSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await paymentStore.GetPaymentTransactionSnapshotAsync(
            command.PaymentTransactionId,
            cancellationToken);
        if (!NeedsReconciliation(snapshot, command.ObservedAt))
        {
            return PaymentSessionReconciliationOutcome.Skipped;
        }

        ProviderPaymentSession? providerSession;
        try
        {
            providerSession = await paymentGateway.GetPaymentSessionAsync(
                snapshot!.ProviderOrderCode!,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return await ScheduleRetryAsync(command, "PROVIDER_LOOKUP_FAILED", ex.Message, cancellationToken);
        }

        return await paymentStore.ExecuteInTransactionAsync(async ct =>
        {
            await paymentStore.AcquirePaymentTransactionLockAsync(command.PaymentTransactionId, ct);
            var payment = await paymentStore.GetPaymentTransactionByIdAsync(command.PaymentTransactionId, ct);
            if (!NeedsReconciliation(payment, command.ObservedAt))
            {
                return PaymentSessionReconciliationOutcome.Skipped;
            }

            await paymentStore.AcquireOrderWorkflowLockAsync(payment!.OrderId, ct);
            await paymentStore.ReloadOrderAsync(payment.Order, ct);

            payment.MarkAttempted(command.ObservedAt);
            if (providerSession is null)
            {
                payment.MarkFailed(
                    "PROVIDER_SESSION_NOT_FOUND",
                    "PayOS has no payment session for the persisted provider order code.",
                    command.ObservedAt);
                return await PersistAsync(payment, PaymentSessionReconciliationOutcome.NotFound, command.ObservedAt, ct);
            }

            if (!string.Equals(
                    providerSession.ProviderOrderCode,
                    payment.ProviderOrderCode,
                    StringComparison.Ordinal))
            {
                payment.MarkFailed(
                    PaymentSessionInterventionPolicy.IdentityMismatchCode,
                    "PayOS returned a different provider order code for the payment transaction.",
                    command.ObservedAt);
                return await PersistAsync(
                    payment, PaymentSessionReconciliationOutcome.IdentityMismatch, command.ObservedAt, ct);
            }

            ApplyProviderMetadata(payment, providerSession);
            if (providerSession.Amount.HasValue && providerSession.Amount.Value != payment.Amount)
            {
                payment.MarkFailed(
                    PaymentSessionInterventionPolicy.AmountMismatchCode,
                    "PayOS payment-session amount does not match the payment transaction.",
                    command.ObservedAt);
                return await PersistAsync(
                    payment, PaymentSessionReconciliationOutcome.AmountMismatch, command.ObservedAt, ct);
            }

            var status = providerSession.ProviderStatus?.Trim().ToUpperInvariant();
            if (status is "CANCELLED" or "CANCELED")
            {
                payment.Cancel(command.ObservedAt);
                MarkOrderPaymentCancelled(payment);
                return await PersistAsync(payment, PaymentSessionReconciliationOutcome.Cancelled, command.ObservedAt, ct);
            }

            if (status == "EXPIRED")
            {
                payment.MarkExpired(command.ObservedAt);
                MarkOrderPaymentCancelled(payment);
                return await PersistAsync(payment, PaymentSessionReconciliationOutcome.Expired, command.ObservedAt, ct);
            }

            if (status == "PAID")
            {
                var outcome = ScheduleRetry(
                    payment,
                    command,
                    "AWAITING_SIGNED_WEBHOOK",
                    "PayOS reports PAID; signed webhook remains authoritative for order fulfillment.");
                var persistedOutcome = outcome == PaymentSessionReconciliationOutcome.RetryExhausted
                    ? PaymentSessionReconciliationOutcome.AwaitingWebhook
                    : outcome;
                return await PersistAsync(payment, persistedOutcome, command.ObservedAt, ct);
            }

            if (HasPaymentInstructions(providerSession) && !HasReachedLocalExpiry(payment, command.ObservedAt))
            {
                payment.CheckoutUrl = providerSession.CheckoutUrl;
                payment.QrCodePayload = providerSession.QrCodePayload;
                payment.NextRetryAt = null;
                payment.LastErrorCode = null;
                payment.LastErrorMessage = null;
                return await PersistAsync(payment, PaymentSessionReconciliationOutcome.Restored, command.ObservedAt, ct);
            }

            if (HasReachedLocalExpiry(payment, command.ObservedAt))
            {
                var expiryOutcome = ScheduleRetry(
                    payment,
                    command,
                    "PROVIDER_EXPIRY_NOT_CONFIRMED",
                    "The local payment expiry has passed but the provider has not confirmed a terminal state.");
                return await PersistAsync(payment, expiryOutcome, command.ObservedAt, ct);
            }

            var retryOutcome = ScheduleRetry(
                payment,
                command,
                "PROVIDER_SESSION_INSTRUCTIONS_MISSING",
                "PayOS session exists but does not expose checkout instructions.");
            return await PersistAsync(payment, retryOutcome, command.ObservedAt, ct);
        }, cancellationToken);
    }

    private async Task<PaymentSessionReconciliationOutcome> ScheduleRetryAsync(
        ReconcilePendingPaymentSessionCommand command,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken) =>
        await paymentStore.ExecuteInTransactionAsync(async ct =>
        {
            await paymentStore.AcquirePaymentTransactionLockAsync(command.PaymentTransactionId, ct);
            var payment = await paymentStore.GetPaymentTransactionByIdAsync(command.PaymentTransactionId, ct);
            if (!NeedsReconciliation(payment, command.ObservedAt))
            {
                return PaymentSessionReconciliationOutcome.Skipped;
            }

            payment!.MarkAttempted(command.ObservedAt);
            var outcome = ScheduleRetry(payment, command, errorCode, errorMessage);
            return await PersistAsync(payment, outcome, command.ObservedAt, ct);
        }, cancellationToken);

    private async Task<PaymentSessionReconciliationOutcome> PersistAsync(
        PaymentTransaction payment,
        PaymentSessionReconciliationOutcome outcome,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken)
    {
        await interventionNotifier.NotifyIfRequiredAsync(payment, outcome, observedAt, cancellationToken);
        await paymentStore.SaveChangesAsync(cancellationToken);
        return outcome;
    }

    private static PaymentSessionReconciliationOutcome ScheduleRetry(
        PaymentTransaction payment,
        ReconcilePendingPaymentSessionCommand command,
        string errorCode,
        string errorMessage)
    {
        if (!payment.CanRetry)
        {
            payment.LastErrorCode = errorCode;
            payment.LastErrorMessage = errorMessage;
            return PaymentSessionReconciliationOutcome.RetryExhausted;
        }

        payment.ScheduleRetry(errorCode, errorMessage, command.NextRetryAt);
        return payment.CanRetry
            ? PaymentSessionReconciliationOutcome.RetryScheduled
            : PaymentSessionReconciliationOutcome.RetryExhausted;
    }

    private static bool NeedsReconciliation(PaymentTransaction? payment, DateTimeOffset observedAt) =>
        payment is not null && PaymentSessionInterventionPolicy.CanReconcile(payment, observedAt);

    private static bool HasReachedLocalExpiry(PaymentTransaction payment, DateTimeOffset observedAt) =>
        payment.ExpiresAt.HasValue && payment.ExpiresAt.Value <= observedAt;

    private static void MarkOrderPaymentCancelled(PaymentTransaction payment)
    {
        if (payment.Order.PaymentStatus is not (PaymentStatus.Paid or PaymentStatus.PartiallyRefunded or PaymentStatus.Refunded))
        {
            payment.Order.MarkPaymentCancelled();
        }
    }

    private static bool HasPaymentInstructions(ProviderPaymentSession session) =>
        !string.IsNullOrWhiteSpace(session.CheckoutUrl) ||
        !string.IsNullOrWhiteSpace(session.QrCodePayload);

    private static void ApplyProviderMetadata(
        PaymentTransaction payment,
        ProviderPaymentSession providerSession)
    {
        payment.ProviderPaymentLinkId = providerSession.ProviderPaymentLinkId ?? payment.ProviderPaymentLinkId;
        payment.ProviderStatus = providerSession.ProviderStatus;
        payment.RawResponseJson = providerSession.RawResponseJson;
    }
}
