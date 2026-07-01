using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Persistence.Jobs;

public sealed class DataRetentionJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DataRetentionOptions _options;
    private readonly ILogger<DataRetentionJob> _logger;

    public DataRetentionJob(
        IServiceScopeFactory scopeFactory,
        IOptions<DataRetentionOptions> options,
        ILogger<DataRetentionJob> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled) return;

        using var timer = new PeriodicTimer(TimeSpan.FromHours(_options.IntervalHours));
        do
        {
            await RunAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var purger = scope.ServiceProvider.GetRequiredService<DataRetentionPurger>();
            var result = await purger.PurgeAsync(DateTimeOffset.UtcNow, cancellationToken);
            _logger.LogInformation(
                "Retention purge deleted {Heartbeats} heartbeats, {DeviceEvents} device events, {OperationLogs} operation logs, {SyncInboxReceipts} processed inbox receipts, and {ExecutionRequestNonces} expired request nonces.",
                result.Heartbeats,
                result.DeviceEvents,
                result.OperationLogs,
                result.SyncInboxReceipts,
                result.ExecutionRequestNonces);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Data retention purge failed.");
        }
    }
}
