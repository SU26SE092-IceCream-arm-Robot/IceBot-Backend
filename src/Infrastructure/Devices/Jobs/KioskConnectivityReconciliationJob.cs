using Application.Devices;
using Application.Devices.Abstractions;
using Application.Devices.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Devices.Jobs;

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
                using var itemScope = _scopeFactory.CreateScope();
                var handler = itemScope.ServiceProvider
                    .GetRequiredService<ReconcileKioskConnectivityCommandHandler>();
                await handler.HandleAsync(new ReconcileKioskConnectivityCommand
                {
                    KioskId = kioskId,
                    ObservedAt = observedAt
                }, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kiosk connectivity reconciliation failed.");
        }
    }
}
