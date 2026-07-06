using Application.Devices.Abstractions;
using Domain.Devices.Entities;
using Domain.Tenants.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Devices.Persistence;

public sealed class DeviceManagementStore : IDeviceManagementStore
{
    private readonly IceBotDbContext _dbContext;

    public DeviceManagementStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Device>> ListAsync(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        string? status,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Devices
            .Include(d => d.DeviceType)
            .Include(d => d.DeviceModel)
            .Include(d => d.Kiosk)
            .AsQueryable();

        if (organizationId.HasValue)
        {
            query = query.Where(d => d.Kiosk != null && d.Kiosk.OrganizationId == organizationId.Value);
        }

        if (storeId.HasValue)
        {
            query = query.Where(d => d.Kiosk != null && d.Kiosk.StoreId == storeId.Value);
        }

        if (kioskId.HasValue)
        {
            query = query.Where(d => d.KioskId == kioskId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<Domain.Devices.Enums.DeviceStatus>(status.Trim(), true, out var deviceStatus))
            {
                query = query.Where(d => d.Status == deviceStatus);
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();
            query = query.Where(d =>
                d.Code.ToLower().Contains(normalized) ||
                d.Name.ToLower().Contains(normalized) ||
                (d.SerialNumber != null && d.SerialNumber.ToLower().Contains(normalized)));
        }

        return await query.AsNoTracking().OrderBy(d => d.Code).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Device>> ListAccessibleAsync(
        IEnumerable<Guid> organizationIds,
        IEnumerable<Guid> storeIds,
        IEnumerable<Guid> kioskIds,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        string? status,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var orgs = organizationIds.ToList();
        var stores = storeIds.ToList();
        var kiosks = kioskIds.ToList();

        var query = _dbContext.Devices
            .Include(d => d.DeviceType)
            .Include(d => d.DeviceModel)
            .Include(d => d.Kiosk)
            .Where(d => d.Kiosk != null && (
                orgs.Contains(d.Kiosk.OrganizationId) ||
                stores.Contains(d.Kiosk.StoreId) ||
                kiosks.Contains(d.KioskId!.Value)
            ));

        if (organizationId.HasValue)
        {
            query = query.Where(d => d.Kiosk != null && d.Kiosk.OrganizationId == organizationId.Value);
        }

        if (storeId.HasValue)
        {
            query = query.Where(d => d.Kiosk != null && d.Kiosk.StoreId == storeId.Value);
        }

        if (kioskId.HasValue)
        {
            query = query.Where(d => d.KioskId == kioskId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<Domain.Devices.Enums.DeviceStatus>(status.Trim(), true, out var deviceStatus))
            {
                query = query.Where(d => d.Status == deviceStatus);
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();
            query = query.Where(d =>
                d.Code.ToLower().Contains(normalized) ||
                d.Name.ToLower().Contains(normalized) ||
                (d.SerialNumber != null && d.SerialNumber.ToLower().Contains(normalized)));
        }

        return await query.AsNoTracking().OrderBy(d => d.Code).ToListAsync(cancellationToken);
    }

    public Task<Device?> GetByIdAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Devices
            .Include(d => d.DeviceType)
            .Include(d => d.DeviceModel)
            .Include(d => d.Kiosk)
            .FirstOrDefaultAsync(d => d.Id == deviceId, cancellationToken);
    }

    public Task<Kiosk?> GetKioskByIdAsync(Guid kioskId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Kiosks
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == kioskId, cancellationToken);
    }

    public Task<bool> DeviceTypeExistsAsync(long deviceTypeId, CancellationToken cancellationToken = default)
    {
        return _dbContext.DeviceTypes
            .AnyAsync(dt => dt.Id == deviceTypeId && dt.IsActive, cancellationToken);
    }

    public Task<bool> DeviceModelExistsForTypeAsync(Guid deviceModelId, long deviceTypeId, CancellationToken cancellationToken = default)
    {
        return _dbContext.DeviceModels
            .AnyAsync(dm => dm.Id == deviceModelId && dm.DeviceTypeId == deviceTypeId, cancellationToken);
    }

    public async Task<IReadOnlyList<DeviceType>> ListDeviceTypesAsync(
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DeviceTypes.AsQueryable();
        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();
            query = query.Where(x => x.Code.ToLower().Contains(normalized) || x.Name.ToLower().Contains(normalized));
        }

        return await query.AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<DeviceType?> GetDeviceTypeAsync(
        long deviceTypeId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DeviceTypes.AsQueryable();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(x => x.Id == deviceTypeId, cancellationToken);
    }

    public Task<bool> DeviceTypeCodeExistsAsync(
        string code,
        long? excludeDeviceTypeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var query = _dbContext.DeviceTypes.Where(x => x.Code.ToUpper() == normalized);
        if (excludeDeviceTypeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeDeviceTypeId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task AddDeviceTypeAsync(DeviceType deviceType, CancellationToken cancellationToken = default)
    {
        await _dbContext.DeviceTypes.AddAsync(deviceType, cancellationToken);
    }

    public async Task<IReadOnlyList<DeviceModel>> ListDeviceModelsAsync(
        long deviceTypeId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.DeviceModels.Where(x => x.DeviceTypeId == deviceTypeId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Code.ToLower().Contains(normalized) ||
                x.Name.ToLower().Contains(normalized) ||
                (x.Manufacturer != null && x.Manufacturer.ToLower().Contains(normalized)));
        }

        return await query.AsNoTracking().OrderBy(x => x.Code).ToListAsync(cancellationToken);
    }

    public Task<DeviceModel?> GetDeviceModelAsync(
        Guid deviceModelId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<DeviceModel> query = asNoTracking
            ? _dbContext.DeviceModels.AsNoTracking()
            : _dbContext.DeviceModels;
        return query.FirstOrDefaultAsync(x => x.Id == deviceModelId, cancellationToken);
    }

    public Task<bool> DeviceModelCodeExistsAsync(
        long deviceTypeId,
        string code,
        Guid? excludeDeviceModelId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var query = _dbContext.DeviceModels
            .Where(x => x.DeviceTypeId == deviceTypeId && x.Code.ToUpper() == normalized);
        if (excludeDeviceModelId.HasValue)
        {
            query = query.Where(x => x.Id != excludeDeviceModelId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task AddDeviceModelAsync(DeviceModel deviceModel, CancellationToken cancellationToken = default)
    {
        await _dbContext.DeviceModels.AddAsync(deviceModel, cancellationToken);
    }

    public Task<bool> DeviceModelIsAssignedAsync(
        Guid deviceModelId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Devices.AnyAsync(device => device.DeviceModelId == deviceModelId, cancellationToken);

    public Task<bool> DeviceModelHasActiveDispenserStatesAsync(
        Guid deviceModelId,
        bool requiringLevelSensor,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.IngredientDispenserStates.Where(state =>
            state.IsActive && state.Device.DeviceModelId == deviceModelId);
        if (requiringLevelSensor)
        {
            query = query.Where(state => state.LevelToQuantityProfileJson != null);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> CodeExistsInKioskAsync(Guid kioskId, string code, Guid? excludeDeviceId = null, CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var query = _dbContext.Devices
            .Where(d => d.KioskId == kioskId && d.Code.ToUpper() == normalized);

        if (excludeDeviceId.HasValue)
        {
            query = query.Where(d => d.Id != excludeDeviceId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> SerialNumberExistsAsync(string serialNumber, Guid? excludeDeviceId = null, CancellationToken cancellationToken = default)
    {
        var normalized = serialNumber.Trim().ToUpperInvariant();
        var query = _dbContext.Devices
            .Where(d => d.SerialNumber != null && d.SerialNumber.ToUpper() == normalized);

        if (excludeDeviceId.HasValue)
        {
            query = query.Where(d => d.Id != excludeDeviceId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Device device, CancellationToken cancellationToken = default)
    {
        await _dbContext.Devices.AddAsync(device, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
