using Application.Orders.Management.Automation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Infrastructure.Operations.Automation;
using System.Diagnostics;

namespace Infrastructure.Orders.Jobs;

public sealed class FulfillmentReminderJob(
    IServiceScopeFactory scopeFactory,
    IOptions<FulfillmentReminderOptions> options,
    ILogger<FulfillmentReminderJob> logger) : BackgroundService
{
    private readonly FulfillmentReminderOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled) return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds));
        do
        {
            await ProcessBatchAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            IReadOnlyList<Guid> ids;
            var candidateFailures = 0;
            var observedAt = DateTimeOffset.UtcNow;
            using (var scope = scopeFactory.CreateScope())
            {
                ids = await scope.ServiceProvider.GetRequiredService<FulfillmentReminderService>()
                    .ListDueIdsAsync(observedAt, _options.BatchSize, cancellationToken);
            }

            foreach (var id in ids)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<FulfillmentReminderService>()
                        .ProcessAsync(id, observedAt, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    candidateFailures++;
                    OperationalAutomationMetrics.RecordCandidateFailure("fulfillment_reminder");
                    logger.LogError(exception,
                        "Fulfillment overdue reminder failed for order item {OrderItemId}.", id);
                }
            }

            OperationalAutomationMetrics.RecordRun(
                "fulfillment_reminder",
                candidateFailures == 0 ? "succeeded" : "partial_failure",
                stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            OperationalAutomationMetrics.RecordRun("fulfillment_reminder", "failed", stopwatch.Elapsed);
            logger.LogError(exception, "Fulfillment overdue reminder batch failed.");
        }
    }
}
