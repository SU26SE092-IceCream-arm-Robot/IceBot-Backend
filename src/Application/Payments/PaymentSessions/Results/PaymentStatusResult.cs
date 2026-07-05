using Domain.Orders.Enums;
using Domain.Payments.Enums;
using System.Text.Json.Serialization;

namespace Application.Payments.PaymentSessions.Results;

public sealed class PaymentStatusResult
{
    public Guid PaymentTransactionId { get; set; }

    public Guid OrderId { get; set; }

    public string Provider { get; set; } = null!;

    [JsonIgnore]
    public PaymentTransactionStatus PaymentTransactionStatus { get; set; }

    [JsonIgnore]
    public PaymentStatus OrderPaymentStatus { get; set; }

    [JsonIgnore]
    public OrderStatus OrderStatus { get; set; }

    public decimal Amount { get; set; }

    public decimal? PaidAmount { get; set; }

    public string Currency { get; set; } = "VND";

    public DateTimeOffset? PaidAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public string CustomerStatus { get; set; } = null!;

    public string CustomerStatusMessage { get; set; } = null!;

    public bool CanRetryPayment { get; set; }

    public bool RequiresStaffSupport { get; set; }
}
