using Domain.Devices.Catalog;

namespace Application.Devices.Catalog.Abstractions;

public interface IDeviceManagementStore
{
    Task<IReadOnlyList<Device>> ListAsync(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        string? status,
        string? search,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Device>> ListAccessibleAsync(
        IEnumerable<Guid> organizationIds,
        IEnumerable<Guid> storeIds,
        IEnumerable<Guid> kioskIds,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        string? status,
        string? search,
        CancellationToken cancellationToken = default);

    Task<Device?> GetByIdAsync(Guid deviceId, CancellationToken cancellationToken = default);

    Task<Device?> GetByKioskIdAsync(
        Guid kioskId,
        Guid deviceId,
        CancellationToken cancellationToken = default);

    Task<Domain.Tenants.Entities.Kiosk?> GetKioskByIdAsync(Guid kioskId, CancellationToken cancellationToken = default);

    Task<bool> DeviceTypeExistsAsync(long deviceTypeId, CancellationToken cancellationToken = default);

    Task<bool> DeviceModelExistsForTypeAsync(Guid deviceModelId, long deviceTypeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceType>> ListDeviceTypesAsync(string? search, bool? isActive, CancellationToken cancellationToken = default);

    Task<DeviceType?> GetDeviceTypeAsync(long deviceTypeId, bool asNoTracking = true, CancellationToken cancellationToken = default);

    Task<bool> DeviceTypeCodeExistsAsync(string code, long? excludeDeviceTypeId = null, CancellationToken cancellationToken = default);

    Task AddDeviceTypeAsync(DeviceType deviceType, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceModel>> ListDeviceModelsAsync(long deviceTypeId, string? search, CancellationToken cancellationToken = default);

    Task<DeviceModel?> GetDeviceModelAsync(Guid deviceModelId, bool asNoTracking = true, CancellationToken cancellationToken = default);

    Task<bool> DeviceModelCodeExistsAsync(long deviceTypeId, string code, Guid? excludeDeviceModelId = null, CancellationToken cancellationToken = default);

    Task AddDeviceModelAsync(DeviceModel deviceModel, CancellationToken cancellationToken = default);

    Task<bool> DeviceModelIsAssignedAsync(Guid deviceModelId, CancellationToken cancellationToken = default);

    Task<bool> DeviceModelHasActiveDispenserStatesAsync(
        Guid deviceModelId,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsInKioskAsync(Guid kioskId, string code, Guid? excludeDeviceId = null, CancellationToken cancellationToken = default);

    Task<bool> SerialNumberExistsAsync(string serialNumber, Guid? excludeDeviceId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Device device, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
