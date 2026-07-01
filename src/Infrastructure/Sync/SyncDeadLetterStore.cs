using Domain.Sync.DeadLetters;
using Domain.Sync.Ingestion;
using Domain.Devices.ExecutionEndpoints;
using Application.Sync.Abstractions;
using Domain.Sync.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Sync;

public sealed class SyncDeadLetterStore : ISyncDeadLetterStore
{
    private readonly IceBotDbContext _db;
    public SyncDeadLetterStore(IceBotDbContext db) => _db = db;

    public async Task<(IReadOnlyList<SyncDeadLetter> Items, int Total)> ListAsync(string? status, string? eventType, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.SyncDeadLetters.AsNoTracking().Include(x => x.Kiosk).AsQueryable();
        if (Enum.TryParse<Domain.Sync.Enums.SyncDeadLetterStatus>(status, true, out var parsed)) query = query.Where(x => x.Status == parsed);
        if (!string.IsNullOrWhiteSpace(eventType)) query = query.Where(x => x.EventType == eventType.Trim());
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.FailedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task<SyncDeadLetter?> GetAsync(Guid id, bool tracked, CancellationToken ct = default)
    {
        var query = _db.SyncDeadLetters.Include(x => x.Kiosk)
            .Include(x => x.SyncEventInbox).Include(x => x.ResolvedByAccount).Include(x => x.RetryAttempts).AsQueryable();
        if (!tracked) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<Domain.Devices.ExecutionEndpoints.KioskExecutionEndpoint?> GetEndpointBySourceNodeAsync(Guid sourceNodeId, CancellationToken ct = default) =>
        _db.KioskExecutionEndpoints.AsNoTracking().FirstOrDefaultAsync(x => x.FullEdgeRuntimeId == sourceNodeId || x.ControllerId == sourceNodeId, ct);

    public async Task<int> GetNextRetryAttemptNumberAsync(Guid id, CancellationToken ct = default) =>
        (await _db.SyncDeadLetterRetryAttempts.Where(x => x.SyncDeadLetterId == id).MaxAsync(x => (int?)x.AttemptNumber, ct) ?? 0) + 1;

    public Task AddRetryAttemptAsync(SyncDeadLetterRetryAttempt attempt, CancellationToken ct = default) =>
        _db.SyncDeadLetterRetryAttempts.AddAsync(attempt, ct).AsTask();
    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
