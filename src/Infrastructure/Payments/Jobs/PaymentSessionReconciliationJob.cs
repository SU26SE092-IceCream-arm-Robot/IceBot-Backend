using Application.Payments.Abstractions;
using Application.Payments.PaymentSessions.Commands;
using Infrastructure.Payments.Observability;
using Infrastructure.Payments.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Payments.Jobs;

public sealed class PaymentSessionReconciliationJob(
    IServiceScopeFactory scopeFactory,
    IOptions<PaymentSessionReconciliationOptions> options,
    ILogger<PaymentSessionReconciliationJob> logger) : BackgroundService
{
    private readonly PaymentSessionReconciliationOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds));
        do
        {
            await ReconcileBatchAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ReconcileBatchAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IPaymentStore>();
            var now = DateTimeOffset.UtcNow;
            var ids = await store.ListPendingPaymentSessionReconciliationIdsAsync(
                now.AddSeconds(-_options.StaleAfterSeconds),
                now,
                _options.BatchSize,
                cancellationToken);

            foreach (var id in ids)
            {
                try
                {
                    using var itemScope = scopeFactory.CreateScope();
                    var itemStore = itemScope.ServiceProvider.GetRequiredService<IPaymentStore>();
                    var snapshot = await itemStore.GetPaymentTransactionSnapshotAsync(id, cancellationToken);
                    if (snapshot is not null)
                    {
                        PaymentSessionReconciliationMetrics.RecordPendingAge(now - snapshot.RequestedAt);
                    }
                    var handler = itemScope.ServiceProvider
                        .GetRequiredService<ReconcilePendingPaymentSessionCommandHandler>();
                    var outcome = await handler.HandleAsync(
                        new ReconcilePendingPaymentSessionCommand(
                            id,
                            now,
                            now.AddSeconds(_options.RetryDelaySeconds)),
                        cancellationToken);
                    PaymentSessionReconciliationMetrics.Record(outcome);
                    if (outcome is PaymentSessionReconciliationOutcome.AwaitingWebhook or
                        PaymentSessionReconciliationOutcome.RetryExhausted or
                        PaymentSessionReconciliationOutcome.IdentityMismatch or
                        PaymentSessionReconciliationOutcome.AmountMismatch)
                    {
                        logger.LogWarning(
                            "Payment transaction {PaymentTransactionId} requires intervention after reconciliation outcome {Outcome}.",
                            id,
                            outcome);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Payment-session reconciliation failed for transaction {PaymentTransactionId}.", id);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Payment-session reconciliation failed.");
        }
    }
}
