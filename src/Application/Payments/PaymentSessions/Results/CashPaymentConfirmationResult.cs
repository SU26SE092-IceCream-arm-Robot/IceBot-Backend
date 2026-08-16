using Domain.Payments.Enums;

namespace Application.Payments.PaymentSessions.Results;

public sealed class CashPaymentConfirmationResult
{
    public Guid OrderId { get; init; }
    public Guid PaymentTransactionId { get; init; }
    public PaymentTransactionStatus PaymentStatus { get; init; }
    public bool AlreadyConfirmed { get; init; }
}
