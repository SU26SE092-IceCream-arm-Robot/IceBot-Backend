namespace Application.Payments.Providers;

/// <summary>
/// Captures the provider response to a payment-session lookup, including a
/// business-level not-found response. A missing session is not an exception,
/// but it is still durable exchange evidence.
/// </summary>
public sealed record ProviderPaymentSessionLookupResult(
    ProviderPaymentSession? Session,
    ProviderExchangeEvidence Evidence)
{
    public bool IsFound => Session is not null;

    public static ProviderPaymentSessionLookupResult Found(
        ProviderPaymentSession session,
        ProviderExchangeEvidence evidence) => new(session, evidence);

    public static ProviderPaymentSessionLookupResult NotFound(
        ProviderExchangeEvidence evidence) => new(null, evidence);
}
