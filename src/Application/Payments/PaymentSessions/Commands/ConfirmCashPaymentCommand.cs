using Application.Identity.Tokens.Claims;
using Application.Payments.PaymentSessions.Requests;

namespace Application.Payments.PaymentSessions.Commands;

public sealed class ConfirmCashPaymentCommand
{
    public Guid OrderId { get; init; }
    public Guid PaymentTransactionId { get; init; }
    public required CurrentUserContext UserContext { get; init; }
    public required ConfirmCashPaymentRequest Request { get; init; }
}
