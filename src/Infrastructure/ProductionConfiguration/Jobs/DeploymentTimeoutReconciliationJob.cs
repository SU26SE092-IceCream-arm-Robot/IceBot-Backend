using Application.ProductionConfiguration.Releases.Commands;
using Application.ProductionConfiguration.Deployments.Commands;
using Application.ProductionConfiguration.Routes.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Infrastructure.Operations.Automation;
using System.Diagnostics;

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
            var observedAt = DateTimeOffset.UtcNow;
            await ReconcileExpiredCommandsAsync(observedAt, batchSize, stoppingToken);
            await ReconcileAcceptedReportTimeoutsAsync(observedAt, batchSize, stoppingToken);
            await ReconcileInstalledActivationTimeoutsAsync(observedAt, batchSize, stoppingToken);

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

    private async Task ReconcileExpiredCommandsAsync(
        DateTimeOffset observedAt,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ReconcileExpiredDeploymentCommandsCommandHandler>();
            var result = await handler.HandleAsync(observedAt, batchSize, cancellationToken);
            foreach (var failure in result.Failures)
            {
                OperationalAutomationMetrics.RecordCandidateFailure("deployment_command_expiry");
                _logger.LogError(
                    "Deployment expiry reconciliation skipped command {CommandId}: {Reason}",
                    failure.CommandId,
                    failure.Reason);
            }

            OperationalAutomationMetrics.RecordRun(
                "deployment_command_expiry",
                result.Failures.Count == 0 ? "succeeded" : "partial_failure",
                stopwatch.Elapsed);
            if (result.ExpiredCommandCount > 0)
            {
                _logger.LogInformation(
                    "Deployment timeout reconciliation processed {ExpiredCommandCount} commands, failed {ReconciledDeploymentCount} pending deployments, and found {MissingDeploymentCount} missing deployment references.",
                    result.ExpiredCommandCount,
                    result.ReconciledDeploymentCount,
                    result.MissingDeploymentCount);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            OperationalAutomationMetrics.RecordRun("deployment_command_expiry", "failed", stopwatch.Elapsed);
            _logger.LogError(exception, "Deployment command expiry reconciliation failed.");
        }
    }

    private async Task ReconcileAcceptedReportTimeoutsAsync(
        DateTimeOffset observedAt,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ReconcileAcceptedDeploymentReportTimeoutsCommandHandler>();
            var result = await handler.HandleAsync(
                observedAt,
                TimeSpan.FromMinutes(Math.Max(_options.AcceptedReportTimeoutMinutes, 1)),
                batchSize,
                cancellationToken);
            OperationalAutomationMetrics.RecordRun("deployment_accepted_report_timeout", "succeeded", stopwatch.Elapsed);
            if (result.TotalCount > 0)
            {
                _logger.LogWarning(
                    "Deployment report timeout reconciliation failed {FullEdgeCount} Full Edge and {ControllerCount} low-cost deployments whose accepted commands received no installation report.",
                    result.FullEdgeDeploymentCount,
                    result.ControllerDeploymentCount);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            OperationalAutomationMetrics.RecordRun("deployment_accepted_report_timeout", "failed", stopwatch.Elapsed);
            _logger.LogError(exception, "Deployment accepted-report timeout reconciliation failed.");
        }
    }

    private async Task ReconcileInstalledActivationTimeoutsAsync(
        DateTimeOffset observedAt,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ReconcileInstalledDeploymentActivationTimeoutsCommandHandler>();
            var result = await handler.HandleAsync(
                observedAt,
                TimeSpan.FromMinutes(Math.Max(_options.InstalledActivationTimeoutMinutes, 1)),
                batchSize,
                cancellationToken);
            OperationalAutomationMetrics.RecordRun("deployment_installed_activation_timeout", "succeeded", stopwatch.Elapsed);
            if (result.TotalCount > 0)
            {
                _logger.LogWarning(
                    "Deployment activation timeout reconciliation failed {FullEdgeCount} Full Edge and {ControllerCount} low-cost deployments that remained installed without activation.",
                    result.FullEdgeDeploymentCount,
                    result.ControllerDeploymentCount);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            OperationalAutomationMetrics.RecordRun("deployment_installed_activation_timeout", "failed", stopwatch.Elapsed);
            _logger.LogError(exception, "Deployment installed-activation timeout reconciliation failed.");
        }
    }
}
