using System.ComponentModel.DataAnnotations;

namespace Application.Payments.PaymentSessions.Requests;

public sealed class CreatePaymentSessionRequest
{
    [Required, StringLength(50)]
    public string PaymentMethodCode { get; init; } = string.Empty;

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal ExpectedAmount { get; init; }

    [Required, StringLength(3, MinimumLength = 3)]
    public string ExpectedCurrency { get; init; } = string.Empty;
}
