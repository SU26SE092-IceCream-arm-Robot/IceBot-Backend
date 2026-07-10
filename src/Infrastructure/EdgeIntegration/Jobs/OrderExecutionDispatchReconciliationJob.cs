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

public sealed class OrderExecutionDispatchReconciliationJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OrderExecutionDispatchOptions _options;
    private readonly ILogger<OrderExecutionDispatchReconciliationJob> _logger;

    public OrderExecutionDispatchReconciliationJob(
        IServiceScopeFactory scopeFactory,
        IOptions<OrderExecutionDispatchOptions> options,
        ILogger<OrderExecutionDispatchReconciliationJob> logger)
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

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.ReconciliationIntervalSeconds));
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
            using var scope = _scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IOrderExecutionDispatchStore>();
            var orderIds = await store.ListReadyOrderIdsWithoutInitialCommandAsync(
                _options.ReconciliationBatchSize,
                cancellationToken);

            foreach (var orderId in orderIds)
            {
                using var itemScope = _scopeFactory.CreateScope();
                var handler = itemScope.ServiceProvider.GetRequiredService<DispatchOrderExecutionCommandHandler>();
                var result = await handler.HandleAsync(new DispatchOrderExecutionCommand
                {
                    OrderId = orderId,
                    DispatchAttemptNo = 1
                }, cancellationToken);

                if (!result.Succeeded)
                {
                    _logger.LogWarning(
                        "Order execution dispatch reconciliation deferred order {OrderId}: {Message}",
                        orderId,
                        result.Message);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order execution dispatch reconciliation failed.");
        }
    }
}
