namespace Application.Payments.Providers;

public sealed class ProviderPaymentSessionCreationException : Exception
{
    public ProviderPaymentSessionCreationException(
        string message,
        bool outcomeUnknown,
        Exception? innerException = null)
        : base(message, innerException)
    {
        OutcomeUnknown = outcomeUnknown;
    }

    public bool OutcomeUnknown { get; }
}
