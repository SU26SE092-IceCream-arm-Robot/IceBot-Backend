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

namespace Infrastructure.Devices.ExecutionEndpoints.Persistence;

public sealed class ExecutionEndpointStore : IExecutionEndpointStore
{
    private readonly IceBotDbContext _dbContext;

    public ExecutionEndpointStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> ExecuteMqttCredentialMutationAsync<T>(
        Guid endpointId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"mqtt-credential:{endpointId:D}"}, 0));",
            cancellationToken);
        try
        {
            // Each broker workflow phase must compare against database state, not an
            // entity retained by this scoped DbContext from an earlier phase.
            _dbContext.ChangeTracker.Clear();
            var result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<IReadOnlyList<KioskExecutionEndpoint>> ListAsync(
        Guid? organizationId, Guid? storeId, Guid? kioskId, string? profile, string? status,
        CancellationToken cancellationToken = default)
    {
        return await ApplyFilters(BaseReadQuery(), organizationId, storeId, kioskId, profile, status)
            .AsNoTracking()
            .OrderBy(endpoint => endpoint.Kiosk.Code)
            .ThenBy(endpoint => endpoint.EndpointCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KioskExecutionEndpoint>> ListAccessibleAsync(
        IEnumerable<Guid> organizationIds, IEnumerable<Guid> storeIds, IEnumerable<Guid> kioskIds,
        Guid? organizationId, Guid? storeId, Guid? kioskId, string? profile, string? status,
        CancellationToken cancellationToken = default)
    {
        var organizations = organizationIds.ToArray();
        var stores = storeIds.ToArray();
        var kiosks = kioskIds.ToArray();
        var query = BaseReadQuery().Where(endpoint =>
            organizations.Contains(endpoint.Kiosk.OrganizationId) ||
            stores.Contains(endpoint.Kiosk.StoreId) ||
            kiosks.Contains(endpoint.KioskId));

        return await ApplyFilters(query, organizationId, storeId, kioskId, profile, status)
            .AsNoTracking()
            .OrderBy(endpoint => endpoint.Kiosk.Code)
            .ThenBy(endpoint => endpoint.EndpointCode)
            .ToListAsync(cancellationToken);
    }

    public Task<KioskExecutionEndpoint?> GetByIdAsync(Guid endpointId, CancellationToken cancellationToken = default)
    {
        return BaseReadQuery().FirstOrDefaultAsync(endpoint => endpoint.Id == endpointId, cancellationToken);
    }

    public Task<KioskExecutionEndpoint?> GetByKioskIdAsync(
        Guid kioskId,
        Guid endpointId,
        CancellationToken cancellationToken = default)
    {
        return BaseReadQuery().FirstOrDefaultAsync(
            endpoint => endpoint.Id == endpointId && endpoint.KioskId == kioskId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ExecutionEndpointReadinessProjection>> ListReadinessAsync(
        IEnumerable<Guid> endpointIds,
        CancellationToken cancellationToken = default)
    {
        var ids = endpointIds.Distinct().ToArray();
        return await _dbContext.ExecutionEndpointReadinessProjections
            .AsNoTracking()
            .Include(projection => projection.Capabilities)
            .Where(projection => ids.Contains(projection.KioskExecutionEndpointId))
            .ToListAsync(cancellationToken);
    }

    public Task<Domain.Tenants.Entities.Kiosk?> GetKioskByIdAsync(Guid kioskId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Kiosks.WhereNotDeleted().AsNoTracking().FirstOrDefaultAsync(kiosk => kiosk.Id == kioskId, cancellationToken);
    }

    public Task<Device?> GetDeviceByIdAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Devices.WhereNotDeleted().FirstOrDefaultAsync(device => device.Id == deviceId, cancellationToken);
    }

    public Task<Device?> GetDeviceByKioskIdAsync(
        Guid kioskId,
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Devices.WhereNotDeleted().FirstOrDefaultAsync(
            device => device.Id == deviceId && device.KioskId == kioskId,
            cancellationToken);
    }

    public Task<bool> EndpointCodeExistsAsync(Guid kioskId, string endpointCode, CancellationToken cancellationToken = default)
    {
        var normalized = endpointCode.Trim().ToUpperInvariant();
        return _dbContext.KioskExecutionEndpoints.WhereNotDeleted().AnyAsync(endpoint =>
            endpoint.KioskId == kioskId && endpoint.EndpointCode.ToUpper() == normalized,
            cancellationToken);
    }

    public Task<bool> ProfileIdentityExistsAsync(Guid profileIdentity, CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskExecutionEndpoints.WhereNotDeleted().AnyAsync(endpoint =>
            endpoint.FullEdgeRuntimeId == profileIdentity || endpoint.ControllerId == profileIdentity,
            cancellationToken);
    }

    public Task AddAsync(KioskExecutionEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskExecutionEndpoints.AddAsync(endpoint, cancellationToken).AsTask();
    }

    public Task<KioskExecutionEndpoint?> GetByIdForCredentialRotationAsync(
        Guid endpointId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskExecutionEndpoints.WhereNotDeleted()
            .Include(endpoint => endpoint.Kiosk)
            .Include(endpoint => endpoint.CredentialBinding)
            .Include(endpoint => endpoint.MqttCredential)
            .FirstOrDefaultAsync(endpoint => endpoint.Id == endpointId, cancellationToken);
    }

    public Task<KioskExecutionEndpoint?> GetByKioskIdForCredentialRotationAsync(
        Guid kioskId,
        Guid endpointId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskExecutionEndpoints.WhereNotDeleted()
            .Include(endpoint => endpoint.Kiosk)
            .Include(endpoint => endpoint.CredentialBinding)
            .Include(endpoint => endpoint.MqttCredential)
            .FirstOrDefaultAsync(
                endpoint => endpoint.Id == endpointId && endpoint.KioskId == kioskId,
                cancellationToken);
    }

    public Task<bool> CredentialReferenceExistsAsync(
        string credentialReference,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ExecutionEndpointCredentialBindings.AnyAsync(
            credential => credential.CredentialReference == credentialReference,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> ListStaleMqttCredentialEndpointIdsAsync(
        DateTimeOffset staleBefore,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ExecutionEndpointMqttCredentials
            .AsNoTracking()
            .Where(credential =>
                (credential.Status == ExecutionEndpointMqttCredentialStatus.PendingProvision ||
                 credential.Status == ExecutionEndpointMqttCredentialStatus.PendingRotation ||
                 credential.Status == ExecutionEndpointMqttCredentialStatus.PendingRevoke ||
                 credential.Status == ExecutionEndpointMqttCredentialStatus.RevokeFailed) &&
                (credential.UpdatedAt ?? credential.CreatedAt) <= staleBefore)
            .OrderBy(credential => credential.UpdatedAt ?? credential.CreatedAt)
            .ThenBy(credential => credential.Id)
            .Select(credential => credential.KioskExecutionEndpointId)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public Task AddCredentialBindingAsync(
        ExecutionEndpointCredentialBinding credentialBinding,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ExecutionEndpointCredentialBindings.AddAsync(credentialBinding, cancellationToken).AsTask();
    }

    public Task AddMqttCredentialAsync(
        ExecutionEndpointMqttCredential credential,
        CancellationToken cancellationToken = default) =>
        _dbContext.ExecutionEndpointMqttCredentials.AddAsync(credential, cancellationToken).AsTask();

    public void RemoveSupportedRobotTargets(IEnumerable<ExecutionEndpointSupportedRobotTarget> targets)
    {
        _dbContext.ExecutionEndpointSupportedRobotTargets.RemoveRange(targets);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<KioskExecutionEndpoint> BaseReadQuery()
    {
        return _dbContext.KioskExecutionEndpoints.WhereNotDeleted()
            .Include(endpoint => endpoint.Kiosk)
            .Include(endpoint => endpoint.CredentialBinding)
            .Include(endpoint => endpoint.MqttCredential)
            .Include(endpoint => endpoint.SupportedRobotTargets)
                .ThenInclude(target => target.Device)
            .Where(endpoint => endpoint.Kiosk.DeletedAt == null);
    }

    private static IQueryable<KioskExecutionEndpoint> ApplyFilters(
        IQueryable<KioskExecutionEndpoint> query,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        string? profile,
        string? status)
    {
        if (organizationId.HasValue)
            query = query.Where(endpoint => endpoint.Kiosk.OrganizationId == organizationId.Value);
        if (storeId.HasValue)
            query = query.Where(endpoint => endpoint.Kiosk.StoreId == storeId.Value);
        if (kioskId.HasValue)
            query = query.Where(endpoint => endpoint.KioskId == kioskId.Value);
        if (!string.IsNullOrWhiteSpace(profile) && Enum.TryParse<Domain.Devices.ExecutionEndpoints.KioskExecutionProfile>(profile, true, out var parsedProfile))
            query = query.Where(endpoint => endpoint.ExecutionProfile == parsedProfile);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Domain.Devices.ExecutionEndpoints.KioskExecutionEndpointStatus>(status, true, out var parsedStatus))
            query = query.Where(endpoint => endpoint.Status == parsedStatus);
        return query;
    }
}
