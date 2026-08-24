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
        ProviderExchangeEvidence? evidence = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
        Evidence = evidence;
    }

    public ProviderPaymentSessionFailureKind FailureKind { get; }

    public ProviderExchangeEvidence? Evidence { get; }
}
