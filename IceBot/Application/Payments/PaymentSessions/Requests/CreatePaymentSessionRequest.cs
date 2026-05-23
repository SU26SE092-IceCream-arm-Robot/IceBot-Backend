namespace Application.Payments.PaymentSessions.Requests;

public sealed class CreatePaymentSessionRequest
{
    public string? IdempotencyKey { get; set; }

    public string? Description { get; set; }
}
