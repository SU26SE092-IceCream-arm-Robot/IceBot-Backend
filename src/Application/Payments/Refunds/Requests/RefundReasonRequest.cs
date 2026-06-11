using System.ComponentModel.DataAnnotations;

namespace Application.Payments.Refunds.Requests;

public sealed class RefundReasonRequest
{
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}
