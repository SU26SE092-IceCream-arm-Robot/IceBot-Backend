using Application.Payments.PaymentSessions.Requests;

namespace Application.Payments.PaymentSessions.Commands;

public sealed class HandlePaymentProviderNotificationCommand
{
    public HandlePaymentProviderNotificationRequest Request { get; init; } = null!;
}
