using System.ComponentModel.DataAnnotations;

namespace Application.Payments.Refunds.Requests;

public sealed class RequestRefundRequest
{
    public Guid? PaymentTransactionId { get; set; }

    [Required]
    public string RefundMethod { get; set; } = "FullMoneyRefund"; // "FullMoneyRefund" or "Voucher"

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    public string? VoucherCode { get; set; }
    public decimal? VoucherValue { get; set; }
    public string? Note { get; set; }
}
