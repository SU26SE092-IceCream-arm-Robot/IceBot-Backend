using Application.ProductionConfiguration.Releases.Commands;
using Application.ProductionConfiguration.Deployments.Commands;
using Application.ProductionConfiguration.Routes.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.ProductionConfiguration.Jobs;

public sealed class DeploymentTimeoutReconciliationJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DeploymentTimeoutReconciliationOptions _options;
    private readonly ILogger<DeploymentTimeoutReconciliationJob> _logger;

    public DeploymentTimeoutReconciliationJob(
        IServiceScopeFactory scopeFactory,
        IOptions<DeploymentTimeoutReconciliationOptions> options,
        ILogger<DeploymentTimeoutReconciliationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Deployment timeout reconciliation is disabled.");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(_options.IntervalSeconds, 10));
        var batchSize = Math.Clamp(_options.MaxCommandsPerRun, 1, 1000);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<ReconcileExpiredDeploymentCommandsCommandHandler>();
                var reportTimeoutHandler = scope.ServiceProvider.GetRequiredService<ReconcileAcceptedDeploymentReportTimeoutsCommandHandler>();
                var activationTimeoutHandler = scope.ServiceProvider.GetRequiredService<ReconcileInstalledDeploymentActivationTimeoutsCommandHandler>();
                var now = DateTimeOffset.UtcNow;
                var result = await handler.HandleAsync(now, batchSize, stoppingToken);
                var reportTimeoutResult = await reportTimeoutHandler.HandleAsync(
                    now,
                    TimeSpan.FromMinutes(Math.Max(_options.AcceptedReportTimeoutMinutes, 1)),
                    batchSize,
                    stoppingToken);
                var activationTimeoutResult = await activationTimeoutHandler.HandleAsync(
                    now,
                    TimeSpan.FromMinutes(Math.Max(_options.InstalledActivationTimeoutMinutes, 1)),
                    batchSize,
                    stoppingToken);

                if (result.ExpiredCommandCount > 0)
                {
                    _logger.LogInformation(
                        "Deployment timeout reconciliation processed {ExpiredCommandCount} commands, failed {ReconciledDeploymentCount} pending deployments, and found {MissingDeploymentCount} missing deployment references.",
                        result.ExpiredCommandCount,
                        result.ReconciledDeploymentCount,
                        result.MissingDeploymentCount);
                }

                if (reportTimeoutResult.TotalCount > 0)
                {
                    _logger.LogWarning(
                        "Deployment report timeout reconciliation failed {FullEdgeCount} Full Edge and {ControllerCount} low-cost deployments whose accepted commands received no installation report.",
                        reportTimeoutResult.FullEdgeDeploymentCount,
                        reportTimeoutResult.ControllerDeploymentCount);
                }

                if (activationTimeoutResult.TotalCount > 0)
                {
                    _logger.LogWarning(
                        "Deployment activation timeout reconciliation failed {FullEdgeCount} Full Edge and {ControllerCount} low-cost deployments that remained installed without activation.",
                        activationTimeoutResult.FullEdgeDeploymentCount,
                        activationTimeoutResult.ControllerDeploymentCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deployment timeout reconciliation failed.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
