using Application.ProductionConfiguration.Deployments.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.ProductionConfiguration.Jobs;

public sealed class DeploymentFailureNotificationJob(
    IServiceScopeFactory scopeFactory,
    IOptions<DeploymentFailureNotificationOptions> options,
    ILogger<DeploymentFailureNotificationJob> logger) : BackgroundService
{
    private readonly DeploymentFailureNotificationOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled) return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds));
        do
        {
            try
            {
                IReadOnlyList<Guid> ids;
                using (var scope = scopeFactory.CreateScope())
                    ids = await scope.ServiceProvider.GetRequiredService<DeploymentFailureNotificationService>()
                        .ListPendingIdsAsync(_options.BatchSize, stoppingToken);
                foreach (var id in ids)
                {
                    try
                    {
                        using var scope = scopeFactory.CreateScope();
                        await scope.ServiceProvider.GetRequiredService<DeploymentFailureNotificationService>()
                            .ProcessAsync(id, DateTimeOffset.UtcNow, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            "Deployment failure notification reconciliation failed for deployment {DeploymentId}.",
                            id);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Deployment failure notification reconciliation failed."); }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
