using Domain.Devices.ExecutionEndpoints;
using Domain.Inventory.Entities;
using Domain.Orders.Entities;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionExecution.Projections;
using Domain.Sync.Entities;
using Domain.Sync.Ingestion;

namespace Application.EdgeIntegration.Abstractions;

public interface IExecutionReportUnitOfWork :
    IExecutionReportReceiptStore,
    IDeploymentReportStore,
    IProductionExecutionReportStore,
    IExecutionStockEvidenceStore;

public interface IExecutionReportReceiptStore
{
    Task<T> ExecuteReportIngestionAsync<T>(
        Guid sourceExecutorId,
        Guid sourceEventId,
        Guid commandId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);

    Task<KioskExecutionEndpoint?> GetEndpointForReportAuthAsync(Guid endpointId, CancellationToken cancellationToken = default);
    Task<SyncEventInbox?> GetSyncEventByEventIdAsync(Guid sourceExecutorId, Guid eventId, CancellationToken cancellationToken = default);
    Task<EdgeCommand?> GetCommandAsync(Guid commandId, CancellationToken cancellationToken = default);
    Task AddSyncEventAsync(SyncEventInbox syncEvent, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IDeploymentReportStore
{
    Task<KioskConfigurationDeployment?> GetFullEdgeDeploymentAsync(Guid deploymentId, CancellationToken cancellationToken = default);
    Task<ControllerArtifactSetDeployment?> GetControllerArtifactSetDeploymentAsync(Guid deploymentId, CancellationToken cancellationToken = default);
}

public interface IProductionExecutionReportStore
{
    Task<ProductionExecutionRecord?> GetProductionExecutionRecordAsync(
        Guid sourceCommandId,
        Guid sourceProductionJobId,
        CancellationToken cancellationToken = default);
    Task AddProductionExecutionRecordAsync(ProductionExecutionRecord record, CancellationToken cancellationToken = default);
    Task<OrderExecutionRecord?> GetOrderExecutionRecordAsync(Guid sourceCommandId, CancellationToken cancellationToken = default);
    Task AddOrderExecutionRecordAsync(OrderExecutionRecord record, CancellationToken cancellationToken = default);
    Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task AddOrderStatusHistoryAsync(OrderStatusHistory history, CancellationToken cancellationToken = default);
}

public interface IExecutionStockEvidenceStore
{
    Task AcquireStockMovementLocksAsync(IEnumerable<Guid> sourceEventIds, CancellationToken cancellationToken = default);
    Task<IngredientDispenserState?> GetDispenserStateAsync(Guid dispenserStateId, CancellationToken cancellationToken = default);
    Task<bool> IsIngredientExpectedForOrderItemAsync(Guid orderId, Guid orderItemId, Guid ingredientId, CancellationToken cancellationToken = default);
    Task<bool> StockMovementExistsAsync(Guid sourceEventId, CancellationToken cancellationToken = default);
    Task AddStockMovementAsync(StockMovement movement, CancellationToken cancellationToken = default);
}
