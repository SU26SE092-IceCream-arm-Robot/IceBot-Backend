using Application.Devices.Abstractions;
using Domain.Devices.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Devices.Persistence;

public sealed class ExecutionEndpointStore : IExecutionEndpointStore
{
    private readonly IceBotDbContext _dbContext;

    public ExecutionEndpointStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
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

    public Task<Domain.Tenants.Entities.Kiosk?> GetKioskByIdAsync(Guid kioskId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Kiosks.AsNoTracking().FirstOrDefaultAsync(kiosk => kiosk.Id == kioskId, cancellationToken);
    }

    public Task<Device?> GetDeviceByIdAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Devices.FirstOrDefaultAsync(device => device.Id == deviceId, cancellationToken);
    }

    public Task<bool> EndpointCodeExistsAsync(Guid kioskId, string endpointCode, CancellationToken cancellationToken = default)
    {
        var normalized = endpointCode.Trim().ToUpperInvariant();
        return _dbContext.KioskExecutionEndpoints.AnyAsync(endpoint =>
            endpoint.KioskId == kioskId && endpoint.DeletedAt == null && endpoint.EndpointCode.ToUpper() == normalized,
            cancellationToken);
    }

    public Task<bool> ProfileIdentityExistsAsync(Guid profileIdentity, CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskExecutionEndpoints.AnyAsync(endpoint =>
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
        return _dbContext.KioskExecutionEndpoints
            .Include(endpoint => endpoint.Kiosk)
            .Include(endpoint => endpoint.CredentialBinding)
            .FirstOrDefaultAsync(endpoint => endpoint.Id == endpointId, cancellationToken);
    }

    public Task<bool> CredentialReferenceExistsAsync(
        string credentialReference,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ExecutionEndpointCredentialBindings.AnyAsync(
            credential => credential.CredentialReference == credentialReference,
            cancellationToken);
    }

    public Task AddCredentialBindingAsync(
        ExecutionEndpointCredentialBinding credentialBinding,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ExecutionEndpointCredentialBindings.AddAsync(credentialBinding, cancellationToken).AsTask();
    }

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
        return _dbContext.KioskExecutionEndpoints
            .Include(endpoint => endpoint.Kiosk)
            .Include(endpoint => endpoint.CredentialBinding)
            .Include(endpoint => endpoint.SupportedRobotTargets)
                .ThenInclude(target => target.Device);
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
        if (!string.IsNullOrWhiteSpace(profile) && Enum.TryParse<Domain.Devices.Enums.KioskExecutionProfile>(profile, true, out var parsedProfile))
            query = query.Where(endpoint => endpoint.ExecutionProfile == parsedProfile);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Domain.Devices.Enums.KioskExecutionEndpointStatus>(status, true, out var parsedStatus))
            query = query.Where(endpoint => endpoint.Status == parsedStatus);
        return query;
    }
}
