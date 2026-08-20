using Application.Identity.Tokens.Claims;

namespace Application.Payments.Reconciliation;

public class DailyPaymentReconciliationQuery
{
    public CurrentUserContext UserContext { get; init; } = null!;
    public DateOnly Date { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? KioskId { get; init; }
    public string Provider { get; init; } = "PayOS";
}

public sealed class PaymentReconciliationDiscrepanciesQuery : DailyPaymentReconciliationQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed record PaymentReconciliationReadRequest(
    DateTimeOffset From,
    DateTimeOffset To,
    DateTimeOffset EvidenceFreshAfter,
    string Provider,
    Guid? OrganizationId,
    Guid? StoreId,
    Guid? KioskId,
    bool IsSystemAdmin,
    IReadOnlyCollection<Guid> AllowedOrganizationIds,
    IReadOnlyCollection<Guid> AllowedStoreIds,
    IReadOnlyCollection<Guid> AllowedKioskIds);

public sealed class DailyPaymentReconciliationResult
{
    public DateOnly LocalDate { get; init; }
    public string TimeZoneId { get; init; } = null!;
    public string Provider { get; init; } = null!;
    public string Currency { get; init; } = "VND";
    public decimal ExpectedProviderCollectedAmount { get; init; }
    public decimal CashCollectedAmount { get; init; }
    public decimal ProcessedMoneyRefundAmount { get; init; }
    public decimal ExpectedNetCollectedAmount { get; init; }
    public decimal? ProviderConfirmedCollectedAmount { get; init; }
    public decimal? ProviderDifferenceAmount { get; init; }
    public int PaidOrderCount { get; init; }
    public int ProcessedMoneyRefundCount { get; init; }
    public int DiscrepancyCount { get; init; }
    public string Status { get; init; } = null!;
    public string? StatusReason { get; init; }
    public DateTimeOffset? LastEvidenceAt { get; init; }
}

public sealed class PaymentReconciliationDiscrepancyResult
{
    public string Code { get; init; } = null!;
    public string Severity { get; init; } = null!;
    public string RecommendedAction { get; init; } = null!;
    public Guid PaymentTransactionId { get; init; }
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = null!;
    public string Provider { get; init; } = null!;
    public decimal ExpectedAmount { get; init; }
    public decimal? ObservedAmount { get; init; }
    public string? ObservedStatus { get; init; }
    public DateTimeOffset? ObservedAt { get; init; }
}

public sealed record PaymentProviderObservationCandidate(
    Guid PaymentTransactionId,
    string Provider,
    string ProviderOrderCode);
