using Domain.Devices.ExecutionEndpoints;
using Application.Devices.Catalog.Abstractions;
using Application.Devices.ExecutionEndpoints.Abstractions;
using Application.Devices.Telemetry.Abstractions;
using Application.Devices.Connectivity.Abstractions;
using Application.Devices.Credentials.Abstractions;
using Domain.Devices.Catalog;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Devices.ExecutionEndpoints.Projections;

namespace Infrastructure.Devices.Connectivity.Persistence;
public sealed class ExecutionReadinessStore : IExecutionReadinessStore, IExecutionEndpointReportedDeviceStore
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
        _db.KioskExecutionEndpoints
            .Include(x => x.ReportedDevices)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<Device?> GetDeviceByKioskIdAsync(Guid kioskId, Guid deviceId, CancellationToken ct = default) =>
        _db.Devices.WhereNotDeleted().FirstOrDefaultAsync(item => item.Id == deviceId && item.KioskId == kioskId, ct);
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
    public void RemoveReportedDevices(IEnumerable<ExecutionEndpointReportedDevice> devices) =>
        _db.ExecutionEndpointReportedDevices.RemoveRange(devices);
    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
