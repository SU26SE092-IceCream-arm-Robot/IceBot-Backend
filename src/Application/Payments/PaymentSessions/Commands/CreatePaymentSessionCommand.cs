using Application.Payments.PaymentSessions.Requests;

namespace Application.Payments.PaymentSessions.Commands;

public sealed class CreatePaymentSessionCommand
{
    public Guid OrderId { get; init; }
    public CreatePaymentSessionRequest Request { get; init; } = null!;
}
