using Application.Devices.Telemetry;
using Application.Devices;
using Application.Devices.Catalog.Abstractions;
using Application.Devices.ExecutionEndpoints.Abstractions;
using Application.Devices.Telemetry.Abstractions;
using Application.Devices.Connectivity.Abstractions;
using Application.Devices.Credentials.Abstractions;
using Application.Devices.Catalog.Commands;
using Application.Devices.ExecutionEndpoints.Commands;
using Application.Devices.Telemetry.Commands;
using Application.Devices.Connectivity.Commands;
using Application.Devices.Credentials.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Infrastructure.Operations.Automation;
using System.Diagnostics;

namespace Infrastructure.Devices.Connectivity.Jobs;

public sealed class KioskConnectivityReconciliationJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EdgeTelemetryIngestionOptions _options;
    private readonly ILogger<KioskConnectivityReconciliationJob> _logger;

    public KioskConnectivityReconciliationJob(
        IServiceScopeFactory scopeFactory,
        IOptions<EdgeTelemetryIngestionOptions> options,
        ILogger<KioskConnectivityReconciliationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(_options.ConnectivityReconciliationIntervalSeconds));
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
            var observedAt = DateTimeOffset.UtcNow;
            using var scope = _scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IEdgeTelemetryIngestionStore>();
            var kioskIds = await store.ListConnectivityTimeoutCandidateIdsAsync(
                observedAt.AddSeconds(-_options.HeartbeatTimeoutSeconds),
                _options.ConnectivityReconciliationBatchSize,
                cancellationToken);

            foreach (var kioskId in kioskIds)
            {
                try
                {
                    using var itemScope = _scopeFactory.CreateScope();
                    var handler = itemScope.ServiceProvider
                        .GetRequiredService<ReconcileKioskConnectivityCommandHandler>();
                    await handler.HandleAsync(new ReconcileKioskConnectivityCommand
                    {
                        KioskId = kioskId,
                        ObservedAt = observedAt
                    }, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    OperationalAutomationMetrics.RecordCandidateFailure("kiosk_connectivity_reconciliation");
                    _logger.LogError(ex,
                        "Kiosk connectivity reconciliation failed for kiosk {KioskId}.",
                        kioskId);
                }
            }
            OperationalAutomationMetrics.RecordRun(
                "kiosk_connectivity_reconciliation", "succeeded", stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            OperationalAutomationMetrics.RecordRun(
                "kiosk_connectivity_reconciliation", "failed", stopwatch.Elapsed);
            _logger.LogError(ex, "Kiosk connectivity reconciliation failed.");
        }
    }
}
