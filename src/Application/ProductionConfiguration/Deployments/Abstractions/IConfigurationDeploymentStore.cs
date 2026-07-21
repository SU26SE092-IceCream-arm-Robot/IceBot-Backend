using Application.ProductionConfiguration.Deployments.ReadModels;
using Domain.Devices.ExecutionEndpoints;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;
using Domain.Devices.ExecutionEndpoints.Projections;

namespace Application.ProductionConfiguration.Deployments.Abstractions;

public interface IConfigurationDeploymentObservationReader
{
    Task<ConfigurationDeploymentReadModel?> GetConfigurationDeploymentAsync(
        Guid deploymentId, CancellationToken cancellationToken = default);
}

public interface IConfigurationDeploymentStore : IConfigurationDeploymentObservationReader
{
    Task<T> ExecuteDeploymentCreationAsync<T>(Guid executionScopeId, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
    Task<KioskConfigurationDeployment?> GetFullEdgeDeploymentForReconciliationAsync(Guid deploymentId, CancellationToken cancellationToken = default);
    Task<ControllerArtifactSetDeployment?> GetControllerDeploymentForReconciliationAsync(Guid deploymentId, CancellationToken cancellationToken = default);
    Task<KioskConfigurationDeployment?> GetFullEdgeDeploymentByIdempotencyKeyAsync(Guid endpointId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<ControllerArtifactSetDeployment?> GetControllerDeploymentByIdempotencyKeyAsync(Guid endpointId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<int> CountConfigurationDeploymentsAsync(Guid? organizationId, Guid? storeId, Guid? kioskId, Guid? configurationReleaseId, ConfigurationDeploymentProfile? profile, ConfigurationDeploymentReadStatus? status, bool isSystemAdmin, IEnumerable<Guid> allowedOrganizationIds, IEnumerable<Guid> allowedStoreIds, IEnumerable<Guid> allowedKioskIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConfigurationDeploymentReadModel>> ListConfigurationDeploymentsAsync(Guid? organizationId, Guid? storeId, Guid? kioskId, Guid? configurationReleaseId, ConfigurationDeploymentProfile? profile, ConfigurationDeploymentReadStatus? status, bool isSystemAdmin, IEnumerable<Guid> allowedOrganizationIds, IEnumerable<Guid> allowedStoreIds, IEnumerable<Guid> allowedKioskIds, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<ControllerArtifactSetDeployment?> GetControllerArtifactSetDeploymentForRollbackAsync(Guid deploymentId, CancellationToken cancellationToken = default);
    Task<KioskExecutionEndpoint?> GetEndpointForDeploymentAsync(Guid endpointId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KioskExecutionEndpoint>> ListEndpointsForDeploymentAsync(Guid kioskId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExecutionEndpointReadinessProjection>> ListEndpointReadinessAsync(
        IEnumerable<Guid> endpointIds,
        DateTimeOffset receivedAfter,
        CancellationToken cancellationToken = default);
    Task<bool> HasPendingFullEdgeDeploymentAsync(Guid kioskId, CancellationToken cancellationToken = default);
    Task<int> GetNextFullEdgeDeploymentAttemptNoAsync(Guid kioskId, Guid configurationReleaseId, CancellationToken cancellationToken = default);
    Task<bool> HasPendingControllerArtifactSetDeploymentAsync(Guid controllerId, CancellationToken cancellationToken = default);
    Task<bool> HasPendingDeploymentsForReleaseAsync(Guid releaseId, CancellationToken cancellationToken = default);
    Task<bool> HasAnyDeploymentsForReleaseAsync(Guid releaseId, CancellationToken cancellationToken = default);
    Task<int> FailFullEdgeDeploymentsMissingAcceptedCommandReportAsync(DateTimeOffset acceptedBefore, DateTimeOffset observedAt, int maxDeployments, CancellationToken cancellationToken = default);
    Task<int> FailControllerDeploymentsMissingAcceptedCommandReportAsync(DateTimeOffset acceptedBefore, DateTimeOffset observedAt, int maxDeployments, CancellationToken cancellationToken = default);
    Task<int> FailFullEdgeDeploymentsMissingActivationReportAsync(DateTimeOffset installedBefore, DateTimeOffset observedAt, int maxDeployments, CancellationToken cancellationToken = default);
    Task<int> FailControllerDeploymentsMissingActivationReportAsync(DateTimeOffset installedBefore, DateTimeOffset observedAt, int maxDeployments, CancellationToken cancellationToken = default);
    Task<long> GetNextControllerActiveSetVersionAsync(Guid controllerId, CancellationToken cancellationToken = default);
    Task AddFullEdgeDeploymentAsync(KioskConfigurationDeployment deployment, CancellationToken cancellationToken = default);
    Task AddControllerArtifactSetDeploymentAsync(ControllerArtifactSetDeployment deployment, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
