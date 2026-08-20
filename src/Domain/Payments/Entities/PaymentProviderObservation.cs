using Domain.Common;
using Domain.Payments.Enums;

namespace Domain.Payments.Entities;

/// <summary>
/// Immutable, sanitized evidence returned by a provider lookup. It is not a
/// payment callback and must not be used to fulfill an order by itself.
/// </summary>
public sealed class PaymentProviderObservation : BusinessEntity
{
    public Guid PaymentTransactionId { get; set; }

    public string Provider { get; set; } = null!;

    public string ProviderOrderCode { get; set; } = null!;

    public PaymentProviderObservationOutcome Outcome { get; set; }

    public string? ObservedStatus { get; set; }

    public decimal? ObservedAmount { get; set; }

    public decimal? ObservedPaidAmount { get; set; }

    public string? ProviderTransactionId { get; set; }

    public string? FailureCode { get; set; }

    public string? FailureMessage { get; set; }

    public DateTimeOffset ObservedAt { get; set; }

    public DateTimeOffset CloudReceivedAt { get; set; }

    public PaymentTransaction PaymentTransaction { get; set; } = null!;
}
