namespace Application.Payments.Providers;

public enum ProviderPaymentNotificationVerificationFailureKind
{
    InvalidPayload,
    InvalidSignature,
    ConfigurationUnavailable
}

public sealed class ProviderPaymentNotificationVerificationException(
    ProviderPaymentNotificationVerificationFailureKind kind,
    Exception? innerException = null) : Exception("Payment webhook verification failed.", innerException)
{
    public ProviderPaymentNotificationVerificationFailureKind Kind { get; } = kind;
}
