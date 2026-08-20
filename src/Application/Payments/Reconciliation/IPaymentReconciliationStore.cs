namespace Application.Payments.Reconciliation;

public interface IPaymentReconciliationStore
{
    Task<IReadOnlyList<PaymentProviderObservationCandidate>> ListProviderObservationCandidatesAsync(
        string provider, DateTimeOffset requestedAfter, DateTimeOffset freshAfter, int batchSize,
        CancellationToken cancellationToken = default);

    Task RecordProviderObservationAsync(
        Guid paymentTransactionId, string provider, string providerOrderCode,
        Domain.Payments.Enums.PaymentProviderObservationOutcome outcome, string? observedStatus,
        decimal? observedAmount, decimal? observedPaidAmount, string? providerTransactionId,
        string? failureCode, string? failureMessage, DateTimeOffset observedAt,
        CancellationToken cancellationToken = default);

    Task<DailyPaymentReconciliationResult> GetDailySummaryAsync(
        PaymentReconciliationReadRequest request, DateOnly localDate, string timeZoneId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PaymentReconciliationDiscrepancyResult> Items, int TotalCount)> ListDiscrepanciesAsync(
        PaymentReconciliationReadRequest request, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);
}
