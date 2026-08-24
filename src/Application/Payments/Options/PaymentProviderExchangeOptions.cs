namespace Application.Payments.Options;

public sealed class PaymentProviderExchangeOptions
{
    public const string SectionName = "Payments:ProviderExchanges";

    public int StartedTimeoutSeconds { get; set; } = 60;
}
