using Domain.Devices.ExecutionEndpoints;
using Domain.Inventory.Entities;
using Domain.Orders.Entities;
using Domain.Orders.Incidents;
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
    Task AcquireOrderWorkflowLockAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<ProductionExecutionRecord?> GetProductionExecutionRecordAsync(
        Guid sourceCommandId,
        Guid sourceProductionJobId,
        CancellationToken cancellationToken = default);
    Task<List<ProductionExecutionRecord>> ListProductionExecutionRecordsAsync(
        Guid sourceCommandId,
        Guid orderItemId,
        CancellationToken cancellationToken = default);
    Task<List<ProductionExecutionRecord>> ListProductionExecutionRecordsForOrderItemAsync(
        Guid orderId,
        Guid orderItemId,
        CancellationToken cancellationToken = default);
    Task AddProductionExecutionRecordAsync(ProductionExecutionRecord record, CancellationToken cancellationToken = default);
    Task<OrderExecutionRecord?> GetOrderExecutionRecordAsync(Guid sourceCommandId, CancellationToken cancellationToken = default);
    Task AddOrderExecutionRecordAsync(OrderExecutionRecord record, CancellationToken cancellationToken = default);
    Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task AddOrderStatusHistoryAsync(OrderStatusHistory history, CancellationToken cancellationToken = default);
    Task AddOrderItemStatusHistoryAsync(OrderItemStatusHistory history, CancellationToken cancellationToken = default);
    Task<ProductionIncident?> GetProductionIncidentBySourceAsync(
        Guid sourceCommandId, Guid sourceProductionJobId, CancellationToken cancellationToken = default);
    Task AddProductionIncidentAsync(ProductionIncident incident, CancellationToken cancellationToken = default);
}

public interface IExecutionStockEvidenceStore
{
    Task AcquireStockMovementLocksAsync(IEnumerable<Guid> sourceEventIds, CancellationToken cancellationToken = default);
    Task AcquireDispenserMutationLocksAsync(IEnumerable<Guid> dispenserStateIds, CancellationToken cancellationToken = default);
    Task AcquireKioskIngredientInventoryMutationLocksAsync(IEnumerable<Guid> inventoryIds, CancellationToken cancellationToken = default);
    Task<IngredientDispenserState?> GetDispenserStateAsync(Guid dispenserStateId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IngredientDispenserState>> GetDispenserStatesAsync(IReadOnlyCollection<Guid> dispenserStateIds, CancellationToken cancellationToken = default);
    Task<bool> IsIngredientExpectedForOrderItemAsync(Guid orderId, Guid orderItemId, Guid ingredientId, CancellationToken cancellationToken = default);
    Task<StockMovement?> GetStockMovementBySourceEventIdAsync(Guid sourceEventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpectedInventoryRequirement>> ListExpectedInventoryRequirementsAsync(
        Guid orderId, Guid orderItemId, CancellationToken cancellationToken = default);
    Task<List<IngredientDispenserState>> ListActiveDispenserStatesForExpectedConsumptionAsync(
        Guid kioskId, Guid ingredientId, string unit, CancellationToken cancellationToken = default);
    Task<KioskIngredientInventory?> GetKioskIngredientInventoryForExpectedConsumptionAsync(
        Guid kioskId, Guid ingredientId, string unit, CancellationToken cancellationToken = default);
    Task<InventoryReconciliationCase?> GetInventoryReconciliationCaseAsync(
        Guid sourceEventId, Guid ingredientId, string unit, string reasonCode, CancellationToken cancellationToken = default);
    Task AddStockMovementAsync(StockMovement movement, CancellationToken cancellationToken = default);
    Task AddInventoryReconciliationCaseAsync(InventoryReconciliationCase reconciliationCase, CancellationToken cancellationToken = default);
}

public sealed record ExpectedInventoryRequirement(Guid IngredientId, decimal Quantity, string Unit);
