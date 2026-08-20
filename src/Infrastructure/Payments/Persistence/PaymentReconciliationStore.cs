using Application.Payments.Reconciliation;
using Domain.Payments.Entities;
using Domain.Payments.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Payments.Persistence;

public sealed class PaymentReconciliationStore : IPaymentReconciliationStore
{
    private readonly IceBotDbContext _dbContext;

    public PaymentReconciliationStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PaymentProviderObservationCandidate>> ListProviderObservationCandidatesAsync(
        string provider, DateTimeOffset requestedAfter, DateTimeOffset freshAfter, int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentTransactions.WhereNotDeleted().AsNoTracking()
            .Where(payment => payment.Provider == provider && payment.ProviderOrderCode != null &&
                payment.RequestedAt >= requestedAfter &&
                !_dbContext.PaymentProviderObservations.Any(observation =>
                    observation.PaymentTransactionId == payment.Id &&
                    observation.Outcome == PaymentProviderObservationOutcome.Succeeded &&
                    observation.CloudReceivedAt >= freshAfter))
            .OrderBy(payment => payment.RequestedAt)
            .Take(batchSize)
            .Select(payment => new PaymentProviderObservationCandidate(
                payment.Id, payment.Provider, payment.ProviderOrderCode!))
            .ToListAsync(cancellationToken);
    }

    public async Task RecordProviderObservationAsync(
        Guid paymentTransactionId, string provider, string providerOrderCode,
        PaymentProviderObservationOutcome outcome, string? observedStatus,
        decimal? observedAmount, decimal? observedPaidAmount, string? providerTransactionId,
        string? failureCode, string? failureMessage, DateTimeOffset observedAt,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.PaymentProviderObservations.AddAsync(new PaymentProviderObservation
        {
            Id = Guid.NewGuid(),
            PaymentTransactionId = paymentTransactionId,
            Provider = provider,
            ProviderOrderCode = providerOrderCode,
            Outcome = outcome,
            ObservedStatus = observedStatus,
            ObservedAmount = observedAmount,
            ObservedPaidAmount = observedPaidAmount,
            ProviderTransactionId = providerTransactionId,
            FailureCode = failureCode,
            FailureMessage = failureMessage,
            ObservedAt = observedAt,
            CloudReceivedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DailyPaymentReconciliationResult> GetDailySummaryAsync(
        PaymentReconciliationReadRequest request, DateOnly localDate, string timeZoneId,
        CancellationToken cancellationToken = default)
    {
        var data = await LoadDataAsync(request, cancellationToken);
        var primaryProvider = data.PrimaryProviderTransactions;
        var cashAmount = data.PrimaryCashTransactions.Sum(payment => payment.Amount);
        var moneyRefunds = data.MoneyRefunds;
        var expectedProviderAmount = primaryProvider.Sum(payment => payment.Amount);
        var latestEvidence = await LoadLatestEvidenceAsync(primaryProvider.Select(payment => payment.Id), request.Provider, cancellationToken);
        var freshEvidence = primaryProvider
            .Select(payment => latestEvidence.GetValueOrDefault(payment.Id))
            .Where(evidence => evidence is not null && evidence.Outcome == PaymentProviderObservationOutcome.Succeeded && evidence.CloudReceivedAt >= request.EvidenceFreshAfter)
            .Select(evidence => evidence!)
            .ToArray();
        var allEvidenceFresh = primaryProvider.Count == freshEvidence.Length;
        decimal? confirmedAmount = allEvidenceFresh
            ? freshEvidence.Where(IsProviderPaid).Sum(evidence => evidence.ObservedPaidAmount ?? evidence.ObservedAmount ?? 0m)
            : null;
        var discrepancies = BuildDiscrepancies(data, latestEvidence, request.EvidenceFreshAfter);
        var hasActivity = expectedProviderAmount != 0 || cashAmount != 0 || moneyRefunds.Count != 0;
        var status = !hasActivity
            ? "NoActivity"
            : !allEvidenceFresh
                ? "IncompleteEvidence"
                : discrepancies.Any(discrepancy => discrepancy.Severity == "Critical")
                    ? "Mismatch"
                    : "Balanced";

        return new DailyPaymentReconciliationResult
        {
            LocalDate = localDate,
            TimeZoneId = timeZoneId,
            Provider = request.Provider,
            ExpectedProviderCollectedAmount = expectedProviderAmount,
            CashCollectedAmount = cashAmount,
            ProcessedMoneyRefundAmount = moneyRefunds.Sum(refund => refund.Amount),
            ExpectedNetCollectedAmount = expectedProviderAmount + cashAmount - moneyRefunds.Sum(refund => refund.Amount),
            ProviderConfirmedCollectedAmount = confirmedAmount,
            ProviderDifferenceAmount = confirmedAmount.HasValue ? confirmedAmount.Value - expectedProviderAmount : null,
            PaidOrderCount = primaryProvider.Select(payment => payment.OrderId).Distinct().Count(),
            ProcessedMoneyRefundCount = moneyRefunds.Count,
            DiscrepancyCount = discrepancies.Count,
            Status = status,
            StatusReason = status == "IncompleteEvidence" ? "Provider lookup evidence is missing or stale for one or more paid transactions." : null,
            LastEvidenceAt = freshEvidence.Select(evidence => (DateTimeOffset?)evidence.CloudReceivedAt).OrderByDescending(value => value).FirstOrDefault()
        };
    }

    public async Task<(IReadOnlyList<PaymentReconciliationDiscrepancyResult> Items, int TotalCount)> ListDiscrepanciesAsync(
        PaymentReconciliationReadRequest request, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var data = await LoadDataAsync(request, cancellationToken);
        var relevantIds = data.PrimaryProviderTransactions.Select(payment => payment.Id)
            .Concat(data.PendingProviderTransactions.Select(payment => payment.Id))
            .Concat(data.DuplicateProviderTransactions.Select(payment => payment.Id));
        var latestEvidence = await LoadLatestEvidenceAsync(relevantIds, request.Provider, cancellationToken);
        var all = BuildDiscrepancies(data, latestEvidence, request.EvidenceFreshAfter)
            .OrderByDescending(item => item.Severity == "Critical")
            .ThenBy(item => item.OrderNumber, StringComparer.Ordinal)
            .ToArray();
        return (all.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArray(), all.Length);
    }

    private async Task<ReconciliationData> LoadDataAsync(PaymentReconciliationReadRequest request, CancellationToken cancellationToken)
    {
        var allScoped = ApplyScope(_dbContext.PaymentTransactions.WhereNotDeleted().AsNoTracking(), request);
        var paidInPeriod = await allScoped
            .Where(payment => payment.PaidAt >= request.From && payment.PaidAt < request.To &&
                payment.Status == PaymentTransactionStatus.Paid)
            .Select(payment => new PaymentRow(
                payment.Id, payment.OrderId, payment.Order.OrderNumber, payment.Provider,
                payment.PaidAmount ?? payment.Amount, payment.SettlementDisposition, payment.RequestedAt))
            .ToListAsync(cancellationToken);
        var pendingInPeriod = await allScoped
            .Where(payment => payment.Provider == request.Provider && payment.RequestedAt >= request.From && payment.RequestedAt < request.To &&
                (payment.Status == PaymentTransactionStatus.Pending || payment.Status == PaymentTransactionStatus.Authorized))
            .Select(payment => new PaymentRow(payment.Id, payment.OrderId, payment.Order.OrderNumber, payment.Provider,
                payment.Amount, payment.SettlementDisposition, payment.RequestedAt))
            .ToListAsync(cancellationToken);
        var moneyRefunds = await ApplyScope(_dbContext.Refunds.AsNoTracking(), request)
            .Where(refund => refund.Status == RefundStatus.Processed &&
                refund.CompensationMethod == RefundCompensationMethod.FullMoneyRefund &&
                refund.ProcessedAt >= request.From && refund.ProcessedAt < request.To)
            .Select(refund => new RefundRow(refund.Id, refund.PaymentTransactionId, refund.Amount))
            .ToListAsync(cancellationToken);

        return new ReconciliationData(
            paidInPeriod.Where(payment => payment.Provider == request.Provider && payment.Disposition == PaymentSettlementDisposition.Primary).ToList(),
            paidInPeriod.Where(payment => string.Equals(payment.Provider, "Cash", StringComparison.OrdinalIgnoreCase) && payment.Disposition == PaymentSettlementDisposition.Primary).ToList(),
            pendingInPeriod,
            paidInPeriod.Where(payment => payment.Provider == request.Provider && payment.Disposition == PaymentSettlementDisposition.DuplicateRefundRequired).ToList(),
            moneyRefunds);
    }

    private IQueryable<PaymentTransaction> ApplyScope(IQueryable<PaymentTransaction> query, PaymentReconciliationReadRequest request)
    {
        if (request.OrganizationId.HasValue) query = query.Where(payment => payment.Order.OrganizationId == request.OrganizationId.Value);
        if (request.StoreId.HasValue) query = query.Where(payment => payment.Order.StoreId == request.StoreId.Value);
        if (request.KioskId.HasValue) query = query.Where(payment => payment.Order.KioskId == request.KioskId.Value);
        if (!request.IsSystemAdmin)
        {
            query = query.Where(payment =>
                (payment.Order.OrganizationId.HasValue && request.AllowedOrganizationIds.Contains(payment.Order.OrganizationId.Value)) ||
                (payment.Order.StoreId.HasValue && request.AllowedStoreIds.Contains(payment.Order.StoreId.Value)) ||
                request.AllowedKioskIds.Contains(payment.Order.KioskId));
        }
        return query;
    }

    private IQueryable<Refund> ApplyScope(IQueryable<Refund> query, PaymentReconciliationReadRequest request)
    {
        if (request.OrganizationId.HasValue) query = query.Where(refund => refund.PaymentTransaction.Order.OrganizationId == request.OrganizationId.Value);
        if (request.StoreId.HasValue) query = query.Where(refund => refund.PaymentTransaction.Order.StoreId == request.StoreId.Value);
        if (request.KioskId.HasValue) query = query.Where(refund => refund.PaymentTransaction.Order.KioskId == request.KioskId.Value);
        if (!request.IsSystemAdmin)
        {
            query = query.Where(refund =>
                (refund.PaymentTransaction.Order.OrganizationId.HasValue && request.AllowedOrganizationIds.Contains(refund.PaymentTransaction.Order.OrganizationId.Value)) ||
                (refund.PaymentTransaction.Order.StoreId.HasValue && request.AllowedStoreIds.Contains(refund.PaymentTransaction.Order.StoreId.Value)) ||
                request.AllowedKioskIds.Contains(refund.PaymentTransaction.Order.KioskId));
        }
        return query;
    }

    private async Task<Dictionary<Guid, PaymentProviderObservation>> LoadLatestEvidenceAsync(
        IEnumerable<Guid> ids, string provider, CancellationToken cancellationToken)
    {
        var transactionIds = ids.Distinct().ToArray();
        if (transactionIds.Length == 0) return new Dictionary<Guid, PaymentProviderObservation>();
        var observations = await _dbContext.PaymentProviderObservations.AsNoTracking()
            .Where(observation => transactionIds.Contains(observation.PaymentTransactionId) && observation.Provider == provider)
            .OrderByDescending(observation => observation.CloudReceivedAt)
            .ThenByDescending(observation => observation.Id)
            .ToListAsync(cancellationToken);
        return observations.GroupBy(observation => observation.PaymentTransactionId)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private static List<PaymentReconciliationDiscrepancyResult> BuildDiscrepancies(
        ReconciliationData data, IReadOnlyDictionary<Guid, PaymentProviderObservation> evidence,
        DateTimeOffset freshAfter)
    {
        var result = new List<PaymentReconciliationDiscrepancyResult>();
        foreach (var payment in data.PrimaryProviderTransactions)
        {
            evidence.TryGetValue(payment.Id, out var latest);
            if (latest is null || latest.Outcome != PaymentProviderObservationOutcome.Succeeded || latest.CloudReceivedAt < freshAfter)
            {
                result.Add(Create("PROVIDER_EVIDENCE_STALE", "Warning", "Retry provider lookup.", payment, latest));
                continue;
            }
            if (!IsProviderPaid(latest))
            {
                result.Add(Create("LOCAL_PAID_PROVIDER_NOT_PAID", "Critical", "Investigate provider status before settlement action.", payment, latest));
            }
            else if (latest.ObservedPaidAmount.HasValue && latest.ObservedPaidAmount.Value != payment.Amount)
            {
                result.Add(Create("AMOUNT_MISMATCH", "Critical", "Investigate provider amount and payment transaction.", payment, latest));
            }
        }
        foreach (var payment in data.PendingProviderTransactions)
        {
            if (evidence.TryGetValue(payment.Id, out var latest) && latest.Outcome == PaymentProviderObservationOutcome.Succeeded && IsProviderPaid(latest))
                result.Add(Create("PROVIDER_PAID_LOCAL_NOT_APPLIED", "Critical", "Verify signed webhook or use technical payment reconciliation.", payment, latest));
        }
        foreach (var payment in data.DuplicateProviderTransactions)
            result.Add(Create("DUPLICATE_PAYMENT_REFUND_REQUIRED", "Critical", "Resolve the duplicate payment through the refund workflow.", payment, null));
        return result;
    }

    private static PaymentReconciliationDiscrepancyResult Create(string code, string severity, string action, PaymentRow payment, PaymentProviderObservation? evidence) => new()
    {
        Code = code, Severity = severity, RecommendedAction = action, PaymentTransactionId = payment.Id,
        OrderId = payment.OrderId, OrderNumber = payment.OrderNumber, Provider = payment.Provider,
        ExpectedAmount = payment.Amount, ObservedAmount = evidence?.ObservedPaidAmount ?? evidence?.ObservedAmount,
        ObservedStatus = evidence?.ObservedStatus, ObservedAt = evidence?.ObservedAt
    };

    private static bool IsProviderPaid(PaymentProviderObservation observation) =>
        string.Equals(observation.ObservedStatus, "PAID", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(observation.ObservedStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase);

    private sealed record PaymentRow(Guid Id, Guid OrderId, string OrderNumber, string Provider, decimal Amount,
        PaymentSettlementDisposition Disposition, DateTimeOffset RequestedAt);
    private sealed record RefundRow(Guid Id, Guid PaymentTransactionId, decimal Amount);
    private sealed record ReconciliationData(List<PaymentRow> PrimaryProviderTransactions,
        List<PaymentRow> PrimaryCashTransactions, List<PaymentRow> PendingProviderTransactions,
        List<PaymentRow> DuplicateProviderTransactions, List<RefundRow> MoneyRefunds);
}
