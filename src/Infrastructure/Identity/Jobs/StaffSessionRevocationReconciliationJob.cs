using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Identity.Jobs;

public sealed class StaffSessionRevocationReconciliationJob(
    IServiceScopeFactory scopeFactory,
    IOptions<StaffSessionRevocationReconciliationOptions> options,
    ILogger<StaffSessionRevocationReconciliationJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.IntervalSeconds));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var reconciler = scope.ServiceProvider.GetRequiredService<StaffSessionRevocationReconciler>();
                await reconciler.ReconcileAsync(options.Value.BatchSize, stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Staff session revocation reconciliation run failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
