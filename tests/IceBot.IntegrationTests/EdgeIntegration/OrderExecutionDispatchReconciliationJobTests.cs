using Application.EdgeIntegration;
using Application.EdgeIntegration.Abstractions;
using Application.EdgeIntegration.CommandDelivery.Services;
using Application.EdgeIntegration.Dispatch;
using Application.EdgeIntegration.Dispatch.Commands;
using Domain.Devices.ExecutionEndpoints;
using Domain.Devices.ExecutionEndpoints.Projections;
using Domain.Orders.Entities;
using Domain.ProductionConfiguration.Entities;
using Domain.Sync.Entities;
using Infrastructure.EdgeIntegration.Jobs;
using IceBot.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IceBot.IntegrationTests.EdgeIntegration;

public sealed class OrderExecutionDispatchReconciliationJobTests
{
    [Fact]
    public async Task PoisonOrder_DoesNotPreventFollowingOrderFromBeingProcessed()
    {
        var firstOrderId = Guid.NewGuid();
        var secondOrderId = Guid.NewGuid();
        var store = new PoisonFirstDispatchStore(firstOrderId, secondOrderId);
        var options = Options.Create(new OrderExecutionDispatchOptions
        {
            Enabled = true,
            ReconciliationIntervalSeconds = 3600,
            ReconciliationBatchSize = 10
        });
        var services = new ServiceCollection()
            .AddSingleton<IOrderExecutionDispatchStore>(store)
            .AddSingleton<IOptions<OrderExecutionDispatchOptions>>(options)
            .AddSingleton<IEdgeCommandWakeUpPublisher, NoOpEdgeCommandWakeUpPublisher>()
            .AddScoped<DispatchOrderExecutionCommandHandler>()
            .BuildServiceProvider();
        var job = new OrderExecutionDispatchReconciliationJob(
            services.GetRequiredService<IServiceScopeFactory>(),
            options,
            NullLogger<OrderExecutionDispatchReconciliationJob>.Instance);

        await job.StartAsync(CancellationToken.None);
        await store.SecondOrderObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await job.StopAsync(CancellationToken.None);

        Assert.Equal([firstOrderId, secondOrderId], store.AttemptedOrderIds);
    }

    private sealed class PoisonFirstDispatchStore(Guid firstOrderId, Guid secondOrderId)
        : IOrderExecutionDispatchStore
    {
        public List<Guid> AttemptedOrderIds { get; } = [];
        public TaskCompletionSource SecondOrderObserved { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<T> ExecuteSerializedAsync<T>(
            Guid orderId,
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default)
        {
            AttemptedOrderIds.Add(orderId);
            if (orderId == firstOrderId)
                throw new InvalidOperationException("Poison order.");
            if (orderId == secondOrderId)
                SecondOrderObserved.TrySetResult();
            return await action(cancellationToken);
        }

        public Task<IReadOnlyList<Guid>> ListReadyOrderIdsWithoutInitialCommandAsync(
            int maxOrders,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([firstOrderId, secondOrderId]);

        public Task<EdgeCommand?> GetCommandAsync(
            Guid orderId,
            int dispatchAttemptNo,
            CancellationToken cancellationToken = default) => Task.FromResult<EdgeCommand?>(null);

        public Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Order?>(null);

        public Task AcquireEndpointAdmissionLockAsync(Guid endpointId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<KioskExecutionEndpoint>> ListActiveEndpointsAsync(
            Guid kioskId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<KioskExecutionEndpoint>>([]);

        public Task<ExecutionEndpointReadinessProjection?> GetReadinessAsync(
            Guid endpointId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ExecutionEndpointReadinessProjection?>(null);

        public Task<ConfigurationRelease?> GetReleaseAsync(
            Guid releaseId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ConfigurationRelease?>(null);

        public Task<ControllerArtifactSetDeployment?> GetControllerActiveSetAsync(
            Guid deploymentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ControllerArtifactSetDeployment?>(null);

        public Task<IReadOnlySet<Guid>> ListReadyIngredientIdsAsync(
            Guid kioskId,
            IReadOnlyCollection<Guid> ingredientIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());

        public Task<EdgeCommand?> GetLatestCommandAsync(
            Guid orderId,
            CancellationToken cancellationToken = default) => Task.FromResult<EdgeCommand?>(null);

        public Task AddOrderStatusHistoryAsync(
            OrderStatusHistory history,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> CountActiveCommandsAsync(
            Guid endpointId,
            CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task AddCommandAsync(EdgeCommand command, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
