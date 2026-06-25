using Domain.Devices.Entities;

namespace Application.Devices.Abstractions;

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

    Task<Domain.Tenants.Entities.Kiosk?> GetKioskByIdAsync(Guid kioskId, CancellationToken cancellationToken = default);

    Task<bool> DeviceTypeExistsAsync(long deviceTypeId, CancellationToken cancellationToken = default);

    Task<bool> DeviceModelExistsForTypeAsync(Guid deviceModelId, long deviceTypeId, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsInKioskAsync(Guid kioskId, string code, Guid? excludeDeviceId = null, CancellationToken cancellationToken = default);

    Task<bool> SerialNumberExistsAsync(string serialNumber, Guid? excludeDeviceId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Device device, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
