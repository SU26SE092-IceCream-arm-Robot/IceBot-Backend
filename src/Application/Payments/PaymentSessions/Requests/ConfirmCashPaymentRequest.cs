using System.ComponentModel.DataAnnotations;

namespace Application.Payments.PaymentSessions.Requests;

public sealed class ConfirmCashPaymentRequest
{
    [StringLength(500)]
    public string? Note { get; init; }
}
