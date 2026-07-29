using System.Diagnostics;
using Application.Devices.Credentials.Commands;
using Application.Devices.Credentials.Support;
using Application.Devices.ExecutionEndpoints.Abstractions;
using Application.Operations.Alerts.Automation;
using Infrastructure.EdgeIntegration.Mqtt;
using Infrastructure.Devices.Credentials.Observability;
using Infrastructure.Operations.Automation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Devices.Credentials.Jobs;

public sealed class MqttCredentialReconciliationJob(
    IServiceScopeFactory scopeFactory,
    IOptions<MqttCredentialProvisioningOptions> options,
    ILogger<MqttCredentialReconciliationJob> logger) : BackgroundService
{
    private const string JobName = "mqtt_credential_reconciliation";
    private readonly MqttCredentialProvisioningOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("MQTT credential reconciliation is disabled with credential provisioning.");
            return;
        }

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(_options.ReconciliationIntervalSeconds));
        do
        {
            await ReconcileBatchAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ReconcileBatchAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var candidateFailures = 0;
        try
        {
            var observedAt = DateTimeOffset.UtcNow;
            using var scope = scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IExecutionEndpointStore>();
            var alertStore = scope.ServiceProvider
                .GetRequiredService<IMqttCredentialAlertAutomationStore>();
            var staleEndpointIds = await store.ListStaleMqttCredentialEndpointIdsAsync(
                observedAt - MqttCredentialOperationPolicy.PendingOperationLease,
                _options.ReconciliationBatchSize,
                cancellationToken);
            var failureStateEndpointIds = await alertStore.ListFailureStateEndpointIdsAsync(
                _options.ReconciliationBatchSize,
                cancellationToken);
            var activeAlertEndpointIds = await alertStore.ListActiveAlertEndpointIdsAsync(
                _options.ReconciliationBatchSize,
                cancellationToken);
            MqttCredentialReconciliationMetrics.SetStaleCandidateCount(staleEndpointIds.Count);
            var staleEndpointSet = staleEndpointIds.ToHashSet();
            var endpointIds = staleEndpointIds
                .Concat(failureStateEndpointIds)
                .Concat(activeAlertEndpointIds)
                .Distinct()
                .ToArray();

            foreach (var endpointId in endpointIds)
            {
                try
                {
                    MqttCredentialReconciliationOutcome? outcome = null;
                    if (staleEndpointSet.Contains(endpointId))
                    {
                        using var itemScope = scopeFactory.CreateScope();
                        var handler = itemScope.ServiceProvider
                            .GetRequiredService<ReconcileStaleMqttEndpointCredentialCommandHandler>();
                        outcome = await handler.HandleAsync(
                            new ReconcileStaleMqttEndpointCredentialCommand(endpointId, observedAt),
                            cancellationToken);
                        MqttCredentialReconciliationMetrics.RecordOutcome(outcome.Value);
                        LogOutcome(endpointId, outcome.Value);
                        if (outcome == MqttCredentialReconciliationOutcome.RevokeRetryFailed)
                            candidateFailures++;
                    }

                    using var alertScope = scopeFactory.CreateScope();
                    var alertReconciler = alertScope.ServiceProvider
                        .GetRequiredService<MqttCredentialOperationalAlertReconciler>();
                    await alertReconciler.ReconcileAsync(
                        endpointId,
                        outcome,
                        observedAt,
                        cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    candidateFailures++;
                    OperationalAutomationMetrics.RecordCandidateFailure(JobName);
                    logger.LogError(ex,
                        "MQTT credential reconciliation failed for execution endpoint {EndpointId}.",
                        endpointId);
                }
            }

            OperationalAutomationMetrics.RecordRun(
                JobName,
                candidateFailures == 0 ? "succeeded" : "partial_failure",
                stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            OperationalAutomationMetrics.RecordRun(JobName, "failed", stopwatch.Elapsed);
            logger.LogError(ex, "MQTT credential reconciliation failed.");
        }
    }

    private void LogOutcome(Guid endpointId, MqttCredentialReconciliationOutcome outcome)
    {
        switch (outcome)
        {
            case MqttCredentialReconciliationOutcome.ProvisioningMarkedFailed:
            case MqttCredentialReconciliationOutcome.RotationMarkedFailed:
                logger.LogWarning(
                    "Stale MQTT credential operation for endpoint {EndpointId} was marked {Outcome}; operator retry is required.",
                    endpointId,
                    outcome);
                break;
            case MqttCredentialReconciliationOutcome.RevokeRetryFailed:
                OperationalAutomationMetrics.RecordCandidateFailure(JobName);
                logger.LogWarning(
                    "Automatic MQTT credential revocation retry failed for endpoint {EndpointId}.",
                    endpointId);
                break;
            case MqttCredentialReconciliationOutcome.Superseded:
                logger.LogInformation(
                    "MQTT credential reconciliation for endpoint {EndpointId} was superseded by a newer operation.",
                    endpointId);
                break;
        }
    }
}
