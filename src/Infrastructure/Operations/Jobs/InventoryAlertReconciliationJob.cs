using Application.Operations.Alerts.Automation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Infrastructure.Operations.Automation;
using System.Diagnostics;

namespace Infrastructure.Operations.Jobs;

public sealed class InventoryAlertReconciliationJob(
    IServiceScopeFactory scopeFactory,
    IOptions<InventoryAlertAutomationOptions> options,
    ILogger<InventoryAlertReconciliationJob> logger) : BackgroundService
{
    private readonly InventoryAlertAutomationOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled) return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds));
        do
        {
            await ReconcileAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var scope = scopeFactory.CreateScope();
            var reconciler = scope.ServiceProvider.GetRequiredService<InventoryAlertReconciler>();
            var result = await reconciler.ReconcileAsync(DateTimeOffset.UtcNow, cancellationToken);
            for (var failure = 0; failure < result.CandidateFailureCount; failure++)
            {
                OperationalAutomationMetrics.RecordCandidateFailure("inventory_alert_reconciliation");
            }
            OperationalAutomationMetrics.RecordRun(
                "inventory_alert_reconciliation",
                result.CandidateFailureCount == 0 ? "succeeded" : "partial_failure",
                stopwatch.Elapsed);
            if (result.ChangedAlertCount > 0)
            {
                logger.LogInformation(
                    "Inventory alert reconciliation applied {TransitionCount} alert transitions.",
                    result.ChangedAlertCount);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            OperationalAutomationMetrics.RecordRun(
                "inventory_alert_reconciliation", "failed", stopwatch.Elapsed);
            logger.LogError(exception, "Inventory alert reconciliation failed.");
        }
    }
}
