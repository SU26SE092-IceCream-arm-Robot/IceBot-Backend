using Domain.Devices.ExecutionEndpoints;
using Application.Devices.Abstractions;
using Domain.Devices.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Devices.ExecutionEndpoints.Projections;

namespace Infrastructure.Devices.Persistence;
public sealed class ExecutionReadinessStore : IExecutionReadinessStore
{
    private readonly IceBotDbContext _db;
    public ExecutionReadinessStore(IceBotDbContext db) => _db = db;
    public async Task<T> ExecuteSerializedAsync<T>(Guid endpointId, Func<CancellationToken, Task<T>> action, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({$"readiness:{endpointId:D}"}, 0));", ct);
        var result = await action(ct); await tx.CommitAsync(ct); return result;
    }
    public Task<KioskExecutionEndpoint?> GetEndpointAsync(Guid id, CancellationToken ct = default) =>
        _db.KioskExecutionEndpoints.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<ExecutionEndpointReadinessProjection?> GetProjectionAsync(Guid id, bool tracked, CancellationToken ct = default)
    {
        var q = _db.ExecutionEndpointReadinessProjections.Include(x => x.Capabilities).AsQueryable();
        if (!tracked) q = q.AsNoTracking();
        return q.FirstOrDefaultAsync(x => x.KioskExecutionEndpointId == id, ct);
    }
    public Task AddProjectionAsync(ExecutionEndpointReadinessProjection item, CancellationToken ct = default) => _db.ExecutionEndpointReadinessProjections.AddAsync(item, ct).AsTask();
    public void ReplaceCapabilities(ExecutionEndpointReadinessProjection projection, IReadOnlyCollection<ExecutionEndpointCapabilityProjection> capabilities)
    {
        _db.ExecutionEndpointCapabilityProjections.RemoveRange(projection.Capabilities);
        foreach (var item in capabilities) item.ExecutionEndpointReadinessProjectionId = projection.Id;
        _db.ExecutionEndpointCapabilityProjections.AddRange(capabilities);
    }
    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
