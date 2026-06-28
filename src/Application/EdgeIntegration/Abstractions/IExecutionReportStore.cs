using Domain.Devices.Entities;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionExecution.Projections;
using Domain.Sync.Entities;

namespace Application.EdgeIntegration.Abstractions;

public interface IExecutionReportStore
{
    Task<T> ExecuteReportIngestionAsync<T>(
        Guid sourceEventId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);

    Task<KioskExecutionEndpoint?> GetEndpointForReportAuthAsync(
        Guid endpointId,
        CancellationToken cancellationToken = default);

    Task<SyncEventInbox?> GetSyncEventByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task AddSyncEventAsync(
        SyncEventInbox syncEvent,
        CancellationToken cancellationToken = default);

    Task<EdgeCommand?> GetCommandAsync(
        Guid commandId,
        CancellationToken cancellationToken = default);

    Task<KioskConfigurationDeployment?> GetFullEdgeDeploymentAsync(
        Guid deploymentId,
        CancellationToken cancellationToken = default);

    Task<ControllerArtifactSetDeployment?> GetControllerArtifactSetDeploymentAsync(
        Guid deploymentId,
        CancellationToken cancellationToken = default);

    Task<ProductionExecutionRecord?> GetProductionExecutionRecordAsync(
        Guid sourceCommandId,
        Guid? sourceProductionJobId,
        CancellationToken cancellationToken = default);

    Task AddProductionExecutionRecordAsync(
        ProductionExecutionRecord record,
        CancellationToken cancellationToken = default);

    Task<OrderExecutionRecord?> GetOrderExecutionRecordAsync(
        Guid sourceCommandId,
        CancellationToken cancellationToken = default);

    Task AddOrderExecutionRecordAsync(
        OrderExecutionRecord record,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
