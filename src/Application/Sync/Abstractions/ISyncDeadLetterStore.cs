using Domain.Sync.DeadLetters;
using Domain.Devices.ExecutionEndpoints;
using Domain.Devices.Catalog;
using Domain.Sync.Entities;
using Domain.Sync.Enums;

namespace Application.Sync.Abstractions;

public interface ISyncDeadLetterStore
{
    Task<(IReadOnlyList<SyncDeadLetter> Items, int Total)> ListAsync(SyncDeadLetterStatus? status, string? eventType, int page, int pageSize, CancellationToken ct = default);
    Task<SyncDeadLetter?> GetAsync(Guid id, bool tracked, CancellationToken ct = default);
    Task<KioskExecutionEndpoint?> GetEndpointBySourceNodeAsync(Guid sourceNodeId, CancellationToken ct = default);
    Task<int> GetNextRetryAttemptNumberAsync(Guid deadLetterId, CancellationToken ct = default);
    Task AddRetryAttemptAsync(SyncDeadLetterRetryAttempt attempt, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<T> ExecuteSerializedAsync<T>(Guid deadLetterId, Func<CancellationToken, Task<T>> action,
        CancellationToken ct = default);
}
