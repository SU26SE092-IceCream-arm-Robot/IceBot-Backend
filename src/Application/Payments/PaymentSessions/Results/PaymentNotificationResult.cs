using Domain.Payments.Enums;

namespace Application.Payments.PaymentSessions.Results;

public sealed class PaymentNotificationResult
{
    public Guid PaymentTransactionId { get; set; }

    public Guid OrderId { get; set; }

    public PaymentTransactionStatus Status { get; set; }

    public bool AlreadyProcessed { get; set; }
}
