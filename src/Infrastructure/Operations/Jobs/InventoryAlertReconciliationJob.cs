using Application.Operations.Alerts.Automation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
            try
            {
                using var scope = scopeFactory.CreateScope();
                var reconciler = scope.ServiceProvider.GetRequiredService<InventoryAlertReconciler>();
                var changed = await reconciler.ReconcileAsync(DateTimeOffset.UtcNow, stoppingToken);
                if (changed > 0)
                {
                    logger.LogInformation("Inventory alert reconciliation applied {TransitionCount} alert transitions.", changed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Inventory alert reconciliation failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
