using Domain.Devices.ExecutionEndpoints;
using Domain.Devices.Catalog;
using Domain.Orders.Entities;
using Domain.ProductionConfiguration.Entities;
using Domain.Sync.Entities;
using Domain.Devices.ExecutionEndpoints.Projections;
using Domain.ProductionExecution.Projections;

namespace Application.EdgeIntegration.Abstractions;

public interface IOrderExecutionDispatchStore
{
    Task<T> ExecuteSerializedAsync<T>(
        Guid orderId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);

    Task AcquireEndpointAdmissionLockAsync(
        Guid endpointId,
        CancellationToken cancellationToken = default);

    Task AcquireKioskOperationalLockAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default);

    Task<bool> IsKioskOperationalAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default);

    Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KioskExecutionEndpoint>> ListActiveEndpointsAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default);

    Task<ExecutionEndpointReadinessProjection?> GetReadinessAsync(
        Guid endpointId,
        CancellationToken cancellationToken = default);

    Task<ConfigurationRelease?> GetReleaseAsync(Guid releaseId, CancellationToken cancellationToken = default);

    Task<ControllerArtifactSetDeployment?> GetControllerActiveSetAsync(
        Guid deploymentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<Guid>> ListReadyIngredientIdsAsync(
        Guid kioskId,
        IReadOnlyCollection<Guid> ingredientIds,
        CancellationToken cancellationToken = default);

    Task<EdgeCommand?> GetCommandAsync(
        Guid orderId,
        int dispatchAttemptNo,
        CancellationToken cancellationToken = default);

    Task<EdgeCommand?> GetLatestCommandAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<EdgeCommand?> GetCommandByIdAsync(
        Guid commandId,
        CancellationToken cancellationToken = default);

    Task<List<ProductionExecutionRecord>> ListProductionExecutionRecordsForOrderItemAsync(
        Guid orderId,
        Guid orderItemId,
        CancellationToken cancellationToken = default);

    Task AddOrderStatusHistoryAsync(
        OrderStatusHistory history,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveCommandsAsync(
        Guid endpointId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListReadyOrderIdsWithoutInitialCommandAsync(
        int maxOrders,
        CancellationToken cancellationToken = default);

    Task AddCommandAsync(EdgeCommand command, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
