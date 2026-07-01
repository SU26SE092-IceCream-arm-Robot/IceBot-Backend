using Domain.Sync.Ingestion;
using Domain.Sync.Entities;

namespace Application.Devices.Abstractions;

public interface IBatchEventSyncStore
{
    Task<SyncEventInbox?> GetReceiptAsync(Guid sourceNodeId, Guid eventId, CancellationToken cancellationToken = default);

    Task<bool> RecordProcessedReceiptAsync(
        Guid eventId,
        Guid kioskId,
        Guid sourceNodeId,
        string eventType,
        DateTimeOffset occurredAt,
        string aggregateType,
        Guid aggregateId,
        CancellationToken cancellationToken = default);
}
