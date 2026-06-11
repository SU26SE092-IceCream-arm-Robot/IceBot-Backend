namespace Application.Payments.PaymentSessions.Queries;

public sealed class GetPaymentTransactionStatusQuery
{
    public Guid PaymentTransactionId { get; init; }
}
