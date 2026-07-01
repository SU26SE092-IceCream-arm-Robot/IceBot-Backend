using Application.EdgeIntegration.Observability;
using Domain.ProductionExecution.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.EdgeIntegration.Jobs;

public sealed class ExecutionMetricsCollectionJob : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExecutionMetricsCollectionJob> _logger;

    public ExecutionMetricsCollectionJob(
        IServiceScopeFactory scopeFactory,
        ILogger<ExecutionMetricsCollectionJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            await CollectAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CollectAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IceBotDbContext>();
            var active = dbContext.OrderExecutionRecords.AsNoTracking()
                .Where(record => record.Status == ProductionExecutionStatus.Accepted ||
                                 record.Status == ProductionExecutionStatus.Running);
            var stale = await active.CountAsync(
                record => record.ObservationStatus == ExecutionObservationStatus.Stale,
                cancellationToken);
            var unreachable = await active.CountAsync(
                record => record.ObservationStatus == ExecutionObservationStatus.Unreachable,
                cancellationToken);
            IceBotEdgeMetrics.SetObservedExecutionCounts(stale, unreachable);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Execution observability metric collection failed.");
        }
    }
}
