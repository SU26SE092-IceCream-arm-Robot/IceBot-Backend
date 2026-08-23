using Domain.Devices.ClientDevices;
using Domain.Operations.Entities;
using Domain.Tenants.Entities;

namespace Application.ClientDevices.Abstractions;

public interface IClientDeviceStore
{
    Task<Kiosk?> GetKioskAsync(Guid kioskId, CancellationToken cancellationToken = default);
    Task<ClientDevice?> GetByIdAsync(Guid clientDeviceId, bool tracking, CancellationToken cancellationToken = default);
    Task<ClientDevice?> GetByInstallationIdAsync(Guid installationId, bool tracking, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClientDevice>> ListByKioskAsync(Guid kioskId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveCustomerSessionAsync(Guid kioskId, DateTimeOffset observedAt, CancellationToken cancellationToken = default);
    Task AcquireKioskLockAsync(Guid kioskId, CancellationToken cancellationToken = default);
    Task AcquireClientDeviceLockAsync(Guid clientDeviceId, CancellationToken cancellationToken = default);
    Task<ClientDeviceOperationReplay?> GetReplayAsync(Guid kioskId, string operation, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<ClientDeviceOperationReplay?> GetReplayForClientDeviceAsync(Guid clientDeviceId, string operation, string idempotencyKey, CancellationToken cancellationToken = default);
    Task AddClientDeviceAsync(ClientDevice clientDevice, CancellationToken cancellationToken = default);
    Task AddReplayAsync(ClientDeviceOperationReplay replay, CancellationToken cancellationToken = default);
    Task AddOperationLogAsync(OperationLog operationLog, CancellationToken cancellationToken = default);
    Task TryObserveAsync(Guid clientDeviceId, DateTimeOffset observedAt, TimeSpan minimumInterval, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
}
