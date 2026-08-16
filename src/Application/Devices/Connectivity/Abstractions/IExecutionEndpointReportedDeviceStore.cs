using Domain.Devices.Catalog;
using Domain.Devices.ExecutionEndpoints;

namespace Application.Devices.Connectivity.Abstractions;

public interface IExecutionEndpointReportedDeviceStore
{
    Task<T> ExecuteSerializedAsync<T>(Guid endpointId, Func<CancellationToken, Task<T>> action, CancellationToken ct = default);
    Task<KioskExecutionEndpoint?> GetEndpointAsync(Guid endpointId, CancellationToken ct = default);
    Task<Device?> GetDeviceByKioskIdAsync(Guid kioskId, Guid deviceId, CancellationToken ct = default);
    void RemoveReportedDevices(IEnumerable<ExecutionEndpointReportedDevice> devices);
    Task SaveChangesAsync(CancellationToken ct = default);
}
