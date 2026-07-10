using Domain.Sync.Ingestion;
using Domain.Sync.Entities;

namespace Application.Devices.Telemetry.Abstractions;

public interface IProductionEventSyncStore
{
    Task<T> ExecuteHistoryIngestionAsync<T>(
        Guid sourceExecutorId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);

    Task<T> ExecuteSummaryIngestionAsync<T>(
        Guid sourceExecutorId,
        string summaryKind,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);

    Task<SyncEventInbox?> GetEventByIdAsync(Guid sourceExecutorId, Guid eventId, CancellationToken cancellationToken = default);
    Task<SyncEventInbox?> GetEventBySequenceAsync(Guid sourceExecutorId, long sequenceNumber, CancellationToken cancellationToken = default);
    Task<ProductionEventCheckpoint?> GetCheckpointAsync(Guid sourceExecutorId, bool tracked, CancellationToken cancellationToken = default);
    Task<List<SyncEventInbox>> ListContiguousCandidatesAsync(Guid sourceExecutorId, long afterSequenceNumber, CancellationToken cancellationToken = default);
    Task<EdgeStateSummary?> GetStateSummaryAsync(Guid sourceExecutorId, string summaryKind, bool tracked, CancellationToken cancellationToken = default);

    Task AddEventAsync(SyncEventInbox item, CancellationToken cancellationToken = default);
    Task AddCheckpointAsync(ProductionEventCheckpoint checkpoint, CancellationToken cancellationToken = default);
    Task AddStateSummaryAsync(EdgeStateSummary summary, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
