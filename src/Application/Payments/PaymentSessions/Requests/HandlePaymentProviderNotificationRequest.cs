namespace Application.Payments.PaymentSessions.Requests;

public sealed class HandlePaymentProviderNotificationRequest
{
    public string RawPayload { get; set; } = "{}";

    public string? Signature { get; set; }
}
