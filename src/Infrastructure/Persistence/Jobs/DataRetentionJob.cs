using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Infrastructure.Operations.Automation;
using System.Diagnostics;

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
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var purger = scope.ServiceProvider.GetRequiredService<DataRetentionPurger>();
            var result = await purger.PurgeAsync(DateTimeOffset.UtcNow, cancellationToken);
            _logger.LogInformation(
                "Retention purge deleted {Heartbeats} heartbeats, {DeviceEvents} device events, {OperationLogs} operation logs, {SyncInboxReceipts} processed inbox receipts, {ExecutionRequestNonces} expired request nonces, {RefreshTokens} refresh tokens, {PasswordResetRequests} password-reset requests, {AccountInvitations} invitations, and {NotificationDeliveries} terminal notification deliveries.",
                result.Heartbeats,
                result.DeviceEvents,
                result.OperationLogs,
                result.SyncInboxReceipts,
                result.ExecutionRequestNonces,
                result.RefreshTokens,
                result.PasswordResetRequests,
                result.AccountInvitations,
                result.NotificationDeliveries);
            foreach (var failure in result.Failures)
            {
                OperationalAutomationMetrics.RecordCandidateFailure("data_retention");
                _logger.LogError(
                    "Data retention purge failed for category {Category}: {Error}",
                    failure.Category,
                    failure.Error);
            }
            OperationalAutomationMetrics.RecordRun(
                "data_retention",
                result.Failures.Count == 0 ? "succeeded" : "partial_failure",
                stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            OperationalAutomationMetrics.RecordRun("data_retention", "failed", stopwatch.Elapsed);
            _logger.LogError(ex, "Data retention purge failed.");
        }
    }
}
