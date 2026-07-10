using Application.EdgeIntegration;
using Application.EdgeIntegration.Dispatch;
using Application.EdgeIntegration.Reports;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.CommandDelivery.Commands;
using Application.EdgeIntegration.Dispatch.Commands;
using Application.EdgeIntegration.Reports.Commands;
using Application.EdgeIntegration.Timeouts.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.EdgeIntegration.Jobs;

public sealed class OrderExecutionTimeoutReconciliationJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OrderExecutionDispatchOptions _options;
    private readonly ILogger<OrderExecutionTimeoutReconciliationJob> _logger;

    public OrderExecutionTimeoutReconciliationJob(
        IServiceScopeFactory scopeFactory,
        IOptions<OrderExecutionDispatchOptions> options,
        ILogger<OrderExecutionTimeoutReconciliationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.TimeoutReconciliationIntervalSeconds));
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
            var store = scope.ServiceProvider.GetRequiredService<IOrderExecutionTimeoutStore>();
            var commandIds = await store.ListCandidateCommandIdsAsync(
                observedAt,
                observedAt.AddMinutes(-_options.AcceptedReportTimeoutMinutes),
                observedAt.AddMinutes(-_options.RunningReportTimeoutMinutes),
                _options.TimeoutReconciliationBatchSize,
                cancellationToken);

            foreach (var sourceCommandId in commandIds)
            {
                using var itemScope = _scopeFactory.CreateScope();
                var handler = itemScope.ServiceProvider.GetRequiredService<ReconcileOrderExecutionTimeoutCommandHandler>();
                await handler.HandleAsync(new ReconcileOrderExecutionTimeoutCommand
                {
                    SourceCommandId = sourceCommandId,
                    ObservedAt = observedAt
                }, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order execution timeout reconciliation failed.");
        }
    }
}
