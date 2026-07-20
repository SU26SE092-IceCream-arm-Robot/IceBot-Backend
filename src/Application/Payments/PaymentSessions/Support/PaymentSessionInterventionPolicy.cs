using System.Linq.Expressions;
using Application.Payments.PaymentSessions.Commands;
using Domain.Payments.Entities;
using Domain.Payments.Enums;

namespace Application.Payments.PaymentSessions.Support;

public static class PaymentSessionInterventionPolicy
{
    public const string IdentityMismatchCode = "PROVIDER_SESSION_IDENTITY_MISMATCH";
    public const string AmountMismatchCode = "PROVIDER_SESSION_AMOUNT_MISMATCH";

    public static bool CanReconcile(PaymentTransaction payment, DateTimeOffset observedAt) =>
        payment.Status == PaymentTransactionStatus.Pending &&
        !string.IsNullOrWhiteSpace(payment.ProviderOrderCode) &&
        (HasMissingInstructions(payment) || HasReachedLocalExpiry(payment, observedAt));

    public static Expression<Func<PaymentTransaction, bool>> BuildReconciliationCandidatePredicate(
        DateTimeOffset observedAt) =>
        payment =>
            payment.Status == PaymentTransactionStatus.Pending &&
            payment.ProviderOrderCode != null &&
            (((payment.CheckoutUrl == null || payment.CheckoutUrl == "") &&
              (payment.QrCodePayload == null || payment.QrCodePayload == "")) ||
             (payment.ExpiresAt.HasValue && payment.ExpiresAt <= observedAt));

    public static Expression<Func<PaymentTransaction, bool>> BuildQueuePredicate(DateTimeOffset observedAt) =>
        payment =>
            payment.ProviderOrderCode != null &&
            payment.LastErrorCode != null &&
            ((payment.Status == PaymentTransactionStatus.Pending &&
              (((payment.CheckoutUrl == null || payment.CheckoutUrl == "") &&
                (payment.QrCodePayload == null || payment.QrCodePayload == "")) ||
               (payment.ExpiresAt.HasValue && payment.ExpiresAt <= observedAt))) ||
             payment.LastErrorCode == IdentityMismatchCode ||
             payment.LastErrorCode == AmountMismatchCode);

    public static bool RequiresNotification(PaymentSessionReconciliationOutcome outcome) =>
        outcome is PaymentSessionReconciliationOutcome.RetryExhausted or
            PaymentSessionReconciliationOutcome.AwaitingWebhook or
            PaymentSessionReconciliationOutcome.IdentityMismatch or
            PaymentSessionReconciliationOutcome.AmountMismatch;

    private static bool HasMissingInstructions(PaymentTransaction payment) =>
        string.IsNullOrWhiteSpace(payment.CheckoutUrl) &&
        string.IsNullOrWhiteSpace(payment.QrCodePayload);

    private static bool HasReachedLocalExpiry(PaymentTransaction payment, DateTimeOffset observedAt) =>
        payment.ExpiresAt.HasValue && payment.ExpiresAt.Value <= observedAt;
}
