using Domain.Sync.DeadLetters;
using Domain.Devices.ExecutionEndpoints;
using Domain.Devices.Entities;
using Domain.Sync.Entities;

namespace Application.Sync.Abstractions;

public interface ISyncDeadLetterStore
{
    Task<(IReadOnlyList<SyncDeadLetter> Items, int Total)> ListAsync(string? status, string? eventType, int page, int pageSize, CancellationToken ct = default);
    Task<SyncDeadLetter?> GetAsync(Guid id, bool tracked, CancellationToken ct = default);
    Task<KioskExecutionEndpoint?> GetEndpointBySourceNodeAsync(Guid sourceNodeId, CancellationToken ct = default);
    Task<int> GetNextRetryAttemptNumberAsync(Guid deadLetterId, CancellationToken ct = default);
    Task AddRetryAttemptAsync(SyncDeadLetterRetryAttempt attempt, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
