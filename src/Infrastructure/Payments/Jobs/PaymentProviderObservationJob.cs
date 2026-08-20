using Application.Payments.Abstractions;
using Application.Payments.Reconciliation;
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
            try
            {
                var session = await gateway.GetPaymentSessionAsync(candidate.ProviderOrderCode, cancellationToken);
                await store.RecordProviderObservationAsync(
                    candidate.PaymentTransactionId, candidate.Provider, candidate.ProviderOrderCode,
                    session is null ? PaymentProviderObservationOutcome.NotFound : PaymentProviderObservationOutcome.Succeeded,
                    session?.ProviderStatus, session?.Amount, session?.PaidAmount, session?.ProviderTransactionId,
                    session is null ? "PROVIDER_SESSION_NOT_FOUND" : null,
                    session is null ? "Provider did not return a payment session." : null,
                    observedAt, cancellationToken);
                PaymentReconciliationMetrics.RecordOutcome(
                    session is null ? PaymentProviderObservationOutcome.NotFound : PaymentProviderObservationOutcome.Succeeded,
                    candidate.Provider);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
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
