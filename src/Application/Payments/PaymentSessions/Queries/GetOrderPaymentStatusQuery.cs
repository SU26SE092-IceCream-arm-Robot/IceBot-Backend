namespace Application.Payments.PaymentSessions.Queries;

public sealed class GetOrderPaymentStatusQuery
{
    public Guid OrderId { get; init; }
}
