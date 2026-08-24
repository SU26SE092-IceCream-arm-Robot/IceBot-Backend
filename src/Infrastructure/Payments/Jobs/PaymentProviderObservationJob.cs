using Application.Payments.Abstractions;
using Application.Payments.Reconciliation;
using Application.Payments.PaymentSessions.Support;
using Application.Payments.Providers;
using Domain.Payments.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Infrastructure.Payments.Observability;
using System.Diagnostics;

namespace Infrastructure.Payments.Jobs;

/// <summary>
/// Records provider lookup evidence for reconciliation. It deliberately does not
/// mutate order/payment state; signed callbacks remain the fulfillment authority.
/// </summary>
public sealed class PaymentProviderObservationJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<PaymentReconciliationOptions> _options;
    private readonly ILogger<PaymentProviderObservationJob> _logger;

    public PaymentProviderObservationJob(IServiceScopeFactory scopeFactory,
        IOptions<PaymentReconciliationOptions> options,
        ILogger<PaymentProviderObservationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ObserveAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Payment provider observation cycle failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.Value.ObservationIntervalSeconds), stoppingToken);
        }
    }

    private async Task ObserveAsync(CancellationToken cancellationToken)
    {
        var startedAt = Stopwatch.GetTimestamp();
        using var scope = _scopeFactory.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<IPaymentGateway>();
        var store = scope.ServiceProvider.GetRequiredService<IPaymentReconciliationStore>();
        var exchanges = scope.ServiceProvider.GetRequiredService<IPaymentProviderExchangeCoordinator>();
        var options = _options.Value;
        var candidates = await store.ListProviderObservationCandidatesAsync(
            gateway.ProviderCode,
            DateTimeOffset.UtcNow.AddDays(-options.ObservationLookbackDays),
            DateTimeOffset.UtcNow.AddMinutes(-options.EvidenceFreshnessMinutes),
            options.ObservationBatchSize,
            cancellationToken);
        PaymentReconciliationMetrics.RecordCandidateCount(candidates.Count);

        foreach (var candidate in candidates)
        {
            var observedAt = DateTimeOffset.UtcNow;
            var exchangeStart = await exchanges.StartLookupAsync(
                candidate.PaymentTransactionId,
                cancellationToken: cancellationToken);
            if (exchangeStart.State != PaymentProviderExchangeStartState.Started || exchangeStart.ExchangeId is not { } exchangeId)
            {
                continue;
            }

            try
            {
                var lookup = await gateway.GetPaymentSessionAsync(candidate.ProviderOrderCode, cancellationToken);
                var session = lookup.Session;
                await exchanges.CompleteAsync(
                    exchangeId,
                    lookup.IsFound ? PaymentProviderExchangeOutcome.Succeeded : PaymentProviderExchangeOutcome.Rejected,
                    lookup.Evidence,
                    lookup.IsFound ? null : "PROVIDER_SESSION_NOT_FOUND",
                    lookup.IsFound ? null : "Provider did not return a payment session.",
                    null,
                    cancellationToken);
                await store.RecordProviderObservationAsync(
                    candidate.PaymentTransactionId, candidate.Provider, candidate.ProviderOrderCode,
                    lookup.IsFound ? PaymentProviderObservationOutcome.Succeeded : PaymentProviderObservationOutcome.NotFound,
                    session?.ProviderStatus, session?.Amount, session?.PaidAmount, session?.ProviderTransactionId,
                    lookup.IsFound ? null : "PROVIDER_SESSION_NOT_FOUND",
                    lookup.IsFound ? null : "Provider did not return a payment session.",
                    observedAt, cancellationToken);
                PaymentReconciliationMetrics.RecordOutcome(
                    lookup.IsFound ? PaymentProviderObservationOutcome.Succeeded : PaymentProviderObservationOutcome.NotFound,
                    candidate.Provider);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                var evidence = exception is ProviderPaymentSessionLookupException lookupException
                    ? lookupException.Evidence
                    : null;
                await exchanges.CompleteAsync(
                    exchangeId,
                    PaymentProviderExchangeOutcome.OutcomeUnknown,
                    evidence,
                    "PROVIDER_LOOKUP_FAILED",
                    Truncate(exception.Message, 500),
                    null,
                    cancellationToken);
                await store.RecordProviderObservationAsync(
                    candidate.PaymentTransactionId, candidate.Provider, candidate.ProviderOrderCode,
                    PaymentProviderObservationOutcome.Failed, null, null, null, null,
                    "PROVIDER_LOOKUP_FAILED", Truncate(exception.Message, 500), observedAt, cancellationToken);
                PaymentReconciliationMetrics.RecordOutcome(PaymentProviderObservationOutcome.Failed, candidate.Provider);
            }
        }

        PaymentReconciliationMetrics.RecordDuration(Stopwatch.GetElapsedTime(startedAt));

        _logger.LogInformation("Payment provider observation recorded {CandidateCount} lookup result(s) for {Provider}.",
            candidates.Count, gateway.ProviderCode);
    }

    private static string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];
}
