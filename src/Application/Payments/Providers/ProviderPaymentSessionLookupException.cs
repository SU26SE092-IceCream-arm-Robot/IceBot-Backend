namespace Application.Payments.Providers;

public sealed class ProviderPaymentSessionLookupException : Exception
{
    public ProviderPaymentSessionLookupException(
        string message,
        ProviderExchangeEvidence? evidence = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Evidence = evidence;
    }

    public ProviderExchangeEvidence? Evidence { get; }
}
