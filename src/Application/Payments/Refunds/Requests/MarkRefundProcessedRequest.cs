using System.ComponentModel.DataAnnotations;

namespace Application.Payments.Refunds.Requests;

public sealed class MarkRefundProcessedRequest
{
    [StringLength(100)]
    public string? ProviderRefundId { get; set; }
    public bool? MoneyWasRefunded { get; set; }
}
