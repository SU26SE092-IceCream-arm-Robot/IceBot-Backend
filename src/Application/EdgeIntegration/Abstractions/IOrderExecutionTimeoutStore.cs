using Domain.Devices.Entities;
using Domain.Orders.Entities;
using Domain.ProductionExecution.Projections;
using Domain.Sync.Entities;

namespace Application.EdgeIntegration.Abstractions;

public interface IOrderExecutionTimeoutStore
{
    Task<T> ExecuteSerializedAsync<T>(
        Guid sourceCommandId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListCandidateCommandIdsAsync(
        DateTimeOffset observedAt,
        DateTimeOffset acceptedCutoff,
        DateTimeOffset runningCutoff,
        int maxCommands,
        CancellationToken cancellationToken = default);

    Task<EdgeCommand?> GetCommandAsync(Guid sourceCommandId, CancellationToken cancellationToken = default);
    Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderExecutionRecord?> GetOrderExecutionRecordAsync(Guid sourceCommandId, CancellationToken cancellationToken = default);
    Task<KioskHeartbeat?> GetLatestHeartbeatAsync(Guid kioskId, Guid sourceExecutorId, CancellationToken cancellationToken = default);
    Task AddOrderExecutionRecordAsync(OrderExecutionRecord record, CancellationToken cancellationToken = default);
    Task AddOrderStatusHistoryAsync(OrderStatusHistory history, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
