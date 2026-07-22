using Domain.Devices.ExecutionEndpoints;
using Domain.Devices.Catalog;
using Domain.Devices.ExecutionEndpoints.Projections;

namespace Application.Devices.ExecutionEndpoints.Abstractions;

public interface IExecutionEndpointStore
{
    Task<T> ExecuteMqttCredentialMutationAsync<T>(
        Guid endpointId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KioskExecutionEndpoint>> ListAsync(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        string? profile,
        string? status,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KioskExecutionEndpoint>> ListAccessibleAsync(
        IEnumerable<Guid> organizationIds,
        IEnumerable<Guid> storeIds,
        IEnumerable<Guid> kioskIds,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        string? profile,
        string? status,
        CancellationToken cancellationToken = default);

    Task<KioskExecutionEndpoint?> GetByIdAsync(Guid endpointId, CancellationToken cancellationToken = default);

    Task<KioskExecutionEndpoint?> GetByKioskIdAsync(
        Guid kioskId,
        Guid endpointId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExecutionEndpointReadinessProjection>> ListReadinessAsync(
        IEnumerable<Guid> endpointIds,
        CancellationToken cancellationToken = default);

    Task<Domain.Tenants.Entities.Kiosk?> GetKioskByIdAsync(Guid kioskId, CancellationToken cancellationToken = default);

    Task<Device?> GetDeviceByIdAsync(Guid deviceId, CancellationToken cancellationToken = default);

    Task<Device?> GetDeviceByKioskIdAsync(
        Guid kioskId,
        Guid deviceId,
        CancellationToken cancellationToken = default);

    Task<bool> EndpointCodeExistsAsync(Guid kioskId, string endpointCode, CancellationToken cancellationToken = default);

    Task<bool> ProfileIdentityExistsAsync(Guid profileIdentity, CancellationToken cancellationToken = default);

    Task AddAsync(KioskExecutionEndpoint endpoint, CancellationToken cancellationToken = default);

    Task<KioskExecutionEndpoint?> GetByIdForCredentialRotationAsync(
        Guid endpointId,
        CancellationToken cancellationToken = default);

    Task<KioskExecutionEndpoint?> GetByKioskIdForCredentialRotationAsync(
        Guid kioskId,
        Guid endpointId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListStaleMqttCredentialEndpointIdsAsync(
        DateTimeOffset staleBefore,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<bool> CredentialReferenceExistsAsync(
        string credentialReference,
        CancellationToken cancellationToken = default);

    Task AddCredentialBindingAsync(
        ExecutionEndpointCredentialBinding credentialBinding,
        CancellationToken cancellationToken = default);

    Task AddMqttCredentialAsync(
        ExecutionEndpointMqttCredential credential,
        CancellationToken cancellationToken = default);

    void RemoveSupportedRobotTargets(IEnumerable<ExecutionEndpointSupportedRobotTarget> targets);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
