using Domain.Devices.Entities;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;
using Domain.Catalog.Entities;
using Domain.RobotConfiguration.Entities;
using Application.ProductionConfiguration.ReadModels;

namespace Application.ProductionConfiguration.Abstractions;

public interface IProductionConfigurationStore
{
    Task<T> ExecuteDeploymentCreationAsync<T>(
        Guid executionScopeId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);

    Task<ConfigurationRelease?> GetReleaseForPublishAsync(Guid releaseId, CancellationToken cancellationToken = default);

    Task<ConfigurationRelease?> GetPublishedReleaseForDeploymentAsync(Guid releaseId, CancellationToken cancellationToken = default);

    Task<ConfigurationRelease?> GetReleaseByIdAsync(Guid releaseId, CancellationToken cancellationToken = default);

    Task<ConfigurationRelease?> GetReleaseForEditAsync(Guid releaseId, CancellationToken cancellationToken = default);

    Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task<ConfigurationRelease> CreateNextReleaseAsync(
        Guid organizationId,
        Func<long, ConfigurationRelease> releaseFactory,
        CancellationToken cancellationToken = default);

    Task<int> CountReleasesAsync(
        Guid? organizationId,
        ConfigurationReleaseStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConfigurationReleaseSummaryReadModel>> ListReleasesAsync(
        Guid? organizationId,
        ConfigurationReleaseStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductVariant>> ListProductVariantsByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Recipe>> ListRecipesByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RobotProgram>> ListRobotProgramsByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<ConfigurationReleaseAuthoringOptionsReadModel> GetAuthoringOptionsAsync(
        Guid organizationId,
        Guid? productVariantId,
        string? search,
        int limit,
        CancellationToken cancellationToken = default);

    Task<int> CountConfigurationDeploymentsAsync(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        Guid? configurationReleaseId,
        ConfigurationDeploymentProfile? profile,
        ConfigurationDeploymentReadStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds,
        IEnumerable<Guid> allowedStoreIds,
        IEnumerable<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConfigurationDeploymentReadModel>> ListConfigurationDeploymentsAsync(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        Guid? configurationReleaseId,
        ConfigurationDeploymentProfile? profile,
        ConfigurationDeploymentReadStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds,
        IEnumerable<Guid> allowedStoreIds,
        IEnumerable<Guid> allowedKioskIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ConfigurationDeploymentReadModel?> GetConfigurationDeploymentAsync(
        Guid deploymentId,
        CancellationToken cancellationToken = default);

    Task<ControllerArtifactSetDeployment?> GetControllerArtifactSetDeploymentForRollbackAsync(
        Guid deploymentId,
        CancellationToken cancellationToken = default);

    Task<KioskConfigurationDeployment?> GetFullEdgeDeploymentForReconciliationAsync(
        Guid deploymentId,
        CancellationToken cancellationToken = default);

    Task<KioskConfigurationDeployment?> GetFullEdgeDeploymentByIdempotencyKeyAsync(
        Guid endpointId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<ControllerArtifactSetDeployment?> GetControllerDeploymentByIdempotencyKeyAsync(
        Guid endpointId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<ControllerArtifactSetDeployment?> GetControllerDeploymentForReconciliationAsync(
        Guid deploymentId,
        CancellationToken cancellationToken = default);

    Task<KioskExecutionEndpoint?> GetEndpointForDeploymentAsync(Guid endpointId, CancellationToken cancellationToken = default);

    Task<bool> HasPendingFullEdgeDeploymentAsync(Guid kioskId, CancellationToken cancellationToken = default);

    Task<int> GetNextFullEdgeDeploymentAttemptNoAsync(
        Guid kioskId,
        Guid configurationReleaseId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPendingControllerArtifactSetDeploymentAsync(Guid controllerId, CancellationToken cancellationToken = default);

    Task<bool> ReleaseHasPendingDeploymentAsync(
        Guid releaseId,
        CancellationToken cancellationToken = default);

    Task<int> FailFullEdgeDeploymentsMissingAcceptedCommandReportAsync(
        DateTimeOffset acceptedBefore,
        DateTimeOffset observedAt,
        int maxDeployments,
        CancellationToken cancellationToken = default);

    Task<int> FailControllerDeploymentsMissingAcceptedCommandReportAsync(
        DateTimeOffset acceptedBefore,
        DateTimeOffset observedAt,
        int maxDeployments,
        CancellationToken cancellationToken = default);

    Task<int> FailFullEdgeDeploymentsMissingActivationReportAsync(
        DateTimeOffset installedBefore,
        DateTimeOffset observedAt,
        int maxDeployments,
        CancellationToken cancellationToken = default);

    Task<int> FailControllerDeploymentsMissingActivationReportAsync(
        DateTimeOffset installedBefore,
        DateTimeOffset observedAt,
        int maxDeployments,
        CancellationToken cancellationToken = default);

    Task<long> GetNextControllerActiveSetVersionAsync(Guid controllerId, CancellationToken cancellationToken = default);

    Task AddFullEdgeDeploymentAsync(KioskConfigurationDeployment deployment, CancellationToken cancellationToken = default);

    Task AddControllerArtifactSetDeploymentAsync(ControllerArtifactSetDeployment deployment, CancellationToken cancellationToken = default);

    Task SaveReleaseReplacementAsync(
        IReadOnlyCollection<ExecutionRoute> removedRoutes,
        CancellationToken cancellationToken = default);

    Task<ConfigurationReleaseDiscardOutcome> DiscardDraftReleaseAsync(
        ConfigurationRelease release,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}


public enum ConfigurationReleaseDiscardOutcome
{
    Deleted = 1,
    Referenced = 2
}
