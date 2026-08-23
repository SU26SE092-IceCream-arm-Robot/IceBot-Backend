namespace Application.Payments.Providers;

public enum ProviderPaymentSessionFailureKind
{
    OutcomeUnknown,
    Unavailable,
    Rejected
}

public sealed class ProviderPaymentSessionCreationException : Exception
{
    public ProviderPaymentSessionCreationException(
        string message,
        ProviderPaymentSessionFailureKind failureKind,
        Exception? innerException = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
    }

    public ProviderPaymentSessionFailureKind FailureKind { get; }
}
