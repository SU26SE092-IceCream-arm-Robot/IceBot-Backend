using Application.ProductionConfiguration.Deployments.Abstractions;
using Application.ProductionConfiguration.Deployments.ReadModels;
using Domain.Devices.ExecutionEndpoints;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;
using Domain.Sync.Enums;
using Domain.Devices.ExecutionEndpoints.Projections;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ProductionConfiguration.Persistence.Deployments;

public sealed class ConfigurationDeploymentStore : IConfigurationDeploymentStore
{
    private readonly IceBotDbContext _dbContext;

    public ConfigurationDeploymentStore(IceBotDbContext dbContext) => _dbContext = dbContext;

    public async Task<T> ExecuteDeploymentCreationAsync<T>(Guid executionScopeId, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"deployment:{executionScopeId:D}"}, 0));", cancellationToken);
        var result = await action(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public Task<KioskConfigurationDeployment?> GetFullEdgeDeploymentForReconciliationAsync(Guid deploymentId, CancellationToken cancellationToken = default) =>
        _dbContext.KioskConfigurationDeployments.FirstOrDefaultAsync(deployment => deployment.Id == deploymentId, cancellationToken);

    public Task<ControllerArtifactSetDeployment?> GetControllerDeploymentForReconciliationAsync(Guid deploymentId, CancellationToken cancellationToken = default) =>
        _dbContext.ControllerArtifactSetDeployments.FirstOrDefaultAsync(deployment => deployment.Id == deploymentId, cancellationToken);

    public Task<KioskConfigurationDeployment?> GetFullEdgeDeploymentByIdempotencyKeyAsync(Guid endpointId, string idempotencyKey, CancellationToken cancellationToken = default) =>
        _dbContext.KioskConfigurationDeployments.AsNoTracking().FirstOrDefaultAsync(deployment =>
            deployment.KioskExecutionEndpointId == endpointId && deployment.IdempotencyKey == idempotencyKey, cancellationToken);

    public Task<ControllerArtifactSetDeployment?> GetControllerDeploymentByIdempotencyKeyAsync(Guid endpointId, string idempotencyKey, CancellationToken cancellationToken = default) =>
        _dbContext.ControllerArtifactSetDeployments.AsNoTracking().Include(deployment => deployment.Items).FirstOrDefaultAsync(deployment =>
            deployment.KioskExecutionEndpointId == endpointId && deployment.IdempotencyKey == idempotencyKey, cancellationToken);

    public Task<int> CountConfigurationDeploymentsAsync(Guid? organizationId, Guid? storeId, Guid? kioskId, Guid? configurationReleaseId,
        ConfigurationDeploymentProfile? profile, ConfigurationDeploymentReadStatus? status, bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds, IEnumerable<Guid> allowedStoreIds, IEnumerable<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default) =>
        BuildConfigurationDeploymentQuery(organizationId, storeId, kioskId, configurationReleaseId, profile, status,
            isSystemAdmin, allowedOrganizationIds, allowedStoreIds, allowedKioskIds).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<ConfigurationDeploymentReadModel>> ListConfigurationDeploymentsAsync(Guid? organizationId, Guid? storeId, Guid? kioskId,
        Guid? configurationReleaseId, ConfigurationDeploymentProfile? profile, ConfigurationDeploymentReadStatus? status, bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds, IEnumerable<Guid> allowedStoreIds, IEnumerable<Guid> allowedKioskIds,
        int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
        await BuildConfigurationDeploymentQuery(organizationId, storeId, kioskId, configurationReleaseId, profile, status,
                isSystemAdmin, allowedOrganizationIds, allowedStoreIds, allowedKioskIds)
            .OrderByDescending(deployment => deployment.RequestedAt).ThenByDescending(deployment => deployment.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

    public Task<ConfigurationDeploymentReadModel?> GetConfigurationDeploymentAsync(Guid deploymentId, CancellationToken cancellationToken = default) =>
        FullEdgeDeploymentQuery().Where(deployment => deployment.Id == deploymentId)
            .Concat(LowCostDeploymentQuery().Where(deployment => deployment.Id == deploymentId))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<ControllerArtifactSetDeployment?> GetControllerArtifactSetDeploymentForRollbackAsync(Guid deploymentId, CancellationToken cancellationToken = default) =>
        _dbContext.ControllerArtifactSetDeployments.AsNoTracking().Include(deployment => deployment.Items)
            .FirstOrDefaultAsync(deployment => deployment.Id == deploymentId, cancellationToken);

    public Task<KioskExecutionEndpoint?> GetEndpointForDeploymentAsync(Guid endpointId, CancellationToken cancellationToken = default) =>
        _dbContext.KioskExecutionEndpoints.WhereNotDeleted().Include(endpoint => endpoint.Kiosk)
            .Include(endpoint => endpoint.CredentialBinding)
            .Include(endpoint => endpoint.SupportedRobotTargets).ThenInclude(target => target.Device)
            .FirstOrDefaultAsync(endpoint => endpoint.Id == endpointId && endpoint.Kiosk.DeletedAt == null, cancellationToken);

    public async Task<IReadOnlyList<KioskExecutionEndpoint>> ListEndpointsForDeploymentAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default) =>
        await _dbContext.KioskExecutionEndpoints.WhereNotDeleted().AsNoTracking()
            .Include(endpoint => endpoint.Kiosk)
            .Include(endpoint => endpoint.CredentialBinding)
            .Include(endpoint => endpoint.SupportedRobotTargets).ThenInclude(target => target.Device)
            .Where(endpoint => endpoint.KioskId == kioskId && endpoint.Kiosk.DeletedAt == null)
            .OrderBy(endpoint => endpoint.EndpointCode)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ExecutionEndpointReadinessProjection>> ListEndpointReadinessAsync(
        IEnumerable<Guid> endpointIds,
        CancellationToken cancellationToken = default)
    {
        var ids = endpointIds.Distinct().ToArray();
        return await _dbContext.ExecutionEndpointReadinessProjections.AsNoTracking()
            .Include(projection => projection.Capabilities)
            .Where(projection => ids.Contains(projection.KioskExecutionEndpointId))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasPendingFullEdgeDeploymentAsync(Guid kioskId, CancellationToken cancellationToken = default) =>
        _dbContext.KioskConfigurationDeployments.AnyAsync(deployment => deployment.KioskId == kioskId &&
            (deployment.Status == KioskConfigurationDeploymentStatus.Pending || deployment.Status == KioskConfigurationDeploymentStatus.Installed), cancellationToken);

    public async Task<int> GetNextFullEdgeDeploymentAttemptNoAsync(Guid kioskId, Guid configurationReleaseId, CancellationToken cancellationToken = default)
    {
        var maximum = await _dbContext.KioskConfigurationDeployments.Where(deployment =>
            deployment.KioskId == kioskId && deployment.ConfigurationReleaseId == configurationReleaseId)
            .Select(deployment => (int?)deployment.AttemptNo).MaxAsync(cancellationToken);
        return (maximum ?? 0) + 1;
    }

    public Task<bool> HasPendingControllerArtifactSetDeploymentAsync(Guid controllerId, CancellationToken cancellationToken = default) =>
        _dbContext.ControllerArtifactSetDeployments.AnyAsync(deployment => deployment.ControllerId == controllerId &&
            (deployment.Status == ControllerArtifactSetDeploymentStatus.Pending || deployment.Status == ControllerArtifactSetDeploymentStatus.Installed), cancellationToken);

    public async Task<bool> HasPendingDeploymentsForReleaseAsync(Guid releaseId, CancellationToken cancellationToken = default)
    {
        var fullEdge = await _dbContext.KioskConfigurationDeployments.AnyAsync(deployment => deployment.ConfigurationReleaseId == releaseId &&
            (deployment.Status == KioskConfigurationDeploymentStatus.Pending || deployment.Status == KioskConfigurationDeploymentStatus.Installed), cancellationToken);
        return fullEdge || await _dbContext.ControllerArtifactSetDeployments.AnyAsync(deployment => deployment.SourceConfigurationReleaseId == releaseId &&
            (deployment.Status == ControllerArtifactSetDeploymentStatus.Pending || deployment.Status == ControllerArtifactSetDeploymentStatus.Installed), cancellationToken);
    }

    public async Task<bool> HasAnyDeploymentsForReleaseAsync(Guid releaseId, CancellationToken cancellationToken = default)
    {
        var fullEdge = await _dbContext.KioskConfigurationDeployments.AnyAsync(deployment => deployment.ConfigurationReleaseId == releaseId, cancellationToken);
        return fullEdge || await _dbContext.ControllerArtifactSetDeployments.AnyAsync(deployment => deployment.SourceConfigurationReleaseId == releaseId, cancellationToken);
    }

    public Task<int> FailFullEdgeDeploymentsMissingAcceptedCommandReportAsync(DateTimeOffset acceptedBefore, DateTimeOffset observedAt, int maxDeployments, CancellationToken cancellationToken = default) =>
        FailFullEdgeAsync(
            deployment => deployment.Status == KioskConfigurationDeploymentStatus.Pending &&
                _dbContext.EdgeCommands.Any(command => command.CommandType == EdgeCommandType.DeployConfiguration &&
                    command.DeploymentKind == DeploymentCommandTargetKind.FullEdgeConfiguration && command.DeploymentId == deployment.Id &&
                    command.Status == EdgeCommandStatus.Accepted && command.RespondedAt != null && command.RespondedAt < acceptedBefore),
            KioskConfigurationDeploymentStatus.Pending, "ExecutionReportTimeout",
            "The execution endpoint accepted the deployment command but did not report an installation result before the timeout.", observedAt, maxDeployments, false, cancellationToken);

    public Task<int> FailControllerDeploymentsMissingAcceptedCommandReportAsync(DateTimeOffset acceptedBefore, DateTimeOffset observedAt, int maxDeployments, CancellationToken cancellationToken = default) =>
        FailControllerAsync(
            deployment => deployment.Status == ControllerArtifactSetDeploymentStatus.Pending &&
                _dbContext.EdgeCommands.Any(command => command.CommandType == EdgeCommandType.DeployConfiguration &&
                    command.DeploymentKind == DeploymentCommandTargetKind.LowCostArtifactSet && command.DeploymentId == deployment.Id &&
                    command.Status == EdgeCommandStatus.Accepted && command.RespondedAt != null && command.RespondedAt < acceptedBefore),
            ControllerArtifactSetDeploymentStatus.Pending, "ExecutionReportTimeout",
            "The execution endpoint accepted the deployment command but did not report an installation result before the timeout.", observedAt, maxDeployments, false, cancellationToken);

    public Task<int> FailFullEdgeDeploymentsMissingActivationReportAsync(DateTimeOffset installedBefore, DateTimeOffset observedAt, int maxDeployments, CancellationToken cancellationToken = default) =>
        FailFullEdgeAsync(deployment => deployment.Status == KioskConfigurationDeploymentStatus.Installed && deployment.CloudReceivedAt != null && deployment.CloudReceivedAt < installedBefore,
            KioskConfigurationDeploymentStatus.Installed, "ActivationReportTimeout",
            "The execution endpoint installed the deployment but did not report activation before the timeout.", observedAt, maxDeployments, true, cancellationToken);

    public Task<int> FailControllerDeploymentsMissingActivationReportAsync(DateTimeOffset installedBefore, DateTimeOffset observedAt, int maxDeployments, CancellationToken cancellationToken = default) =>
        FailControllerAsync(deployment => deployment.Status == ControllerArtifactSetDeploymentStatus.Installed && deployment.CloudReceivedAt != null && deployment.CloudReceivedAt < installedBefore,
            ControllerArtifactSetDeploymentStatus.Installed, "ActivationReportTimeout",
            "The controller installed the artifact set but did not report activation before the timeout.", observedAt, maxDeployments, true, cancellationToken);

    public async Task<long> GetNextControllerActiveSetVersionAsync(Guid controllerId, CancellationToken cancellationToken = default)
    {
        var maximum = await _dbContext.ControllerArtifactSetDeployments.Where(deployment => deployment.ControllerId == controllerId)
            .Select(deployment => (long?)deployment.ActiveSetVersion).MaxAsync(cancellationToken);
        return (maximum ?? 0) + 1;
    }

    public Task AddFullEdgeDeploymentAsync(KioskConfigurationDeployment deployment, CancellationToken cancellationToken = default) =>
        _dbContext.KioskConfigurationDeployments.AddAsync(deployment, cancellationToken).AsTask();

    public Task AddControllerArtifactSetDeploymentAsync(ControllerArtifactSetDeployment deployment, CancellationToken cancellationToken = default) =>
        _dbContext.ControllerArtifactSetDeployments.AddAsync(deployment, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => _dbContext.SaveChangesAsync(cancellationToken);

    private Task<int> FailFullEdgeAsync(
        System.Linq.Expressions.Expression<Func<KioskConfigurationDeployment, bool>> predicate,
        KioskConfigurationDeploymentStatus expectedStatus, string failureCode, string failureReason,
        DateTimeOffset observedAt, int maxDeployments, bool orderByCloudReceived, CancellationToken cancellationToken)
    {
        var candidates = _dbContext.KioskConfigurationDeployments.Where(predicate);
        var ids = (orderByCloudReceived
                ? candidates.OrderBy(deployment => deployment.CloudReceivedAt)
                : candidates.OrderBy(deployment => deployment.RequestedAt))
            .Take(maxDeployments)
            .Select(deployment => deployment.Id);
        return _dbContext.KioskConfigurationDeployments.Where(deployment => ids.Contains(deployment.Id) && deployment.Status == expectedStatus)
            .ExecuteUpdateAsync(setters => setters.SetProperty(deployment => deployment.Status, KioskConfigurationDeploymentStatus.Failed)
                .SetProperty(deployment => deployment.FailureCode, failureCode).SetProperty(deployment => deployment.FailureReason, failureReason)
                .SetProperty(deployment => deployment.UpdatedAt, observedAt), cancellationToken);
    }

    private Task<int> FailControllerAsync(
        System.Linq.Expressions.Expression<Func<ControllerArtifactSetDeployment, bool>> predicate,
        ControllerArtifactSetDeploymentStatus expectedStatus, string failureCode, string failureReason,
        DateTimeOffset observedAt, int maxDeployments, bool orderByCloudReceived, CancellationToken cancellationToken)
    {
        var candidates = _dbContext.ControllerArtifactSetDeployments.Where(predicate);
        var ids = (orderByCloudReceived
                ? candidates.OrderBy(deployment => deployment.CloudReceivedAt)
                : candidates.OrderBy(deployment => deployment.RequestedAt))
            .Take(maxDeployments)
            .Select(deployment => deployment.Id);
        return _dbContext.ControllerArtifactSetDeployments.Where(deployment => ids.Contains(deployment.Id) && deployment.Status == expectedStatus)
            .ExecuteUpdateAsync(setters => setters.SetProperty(deployment => deployment.Status, ControllerArtifactSetDeploymentStatus.Failed)
                .SetProperty(deployment => deployment.FailureCode, failureCode).SetProperty(deployment => deployment.FailureReason, failureReason)
                .SetProperty(deployment => deployment.UpdatedAt, observedAt), cancellationToken);
    }

    private IQueryable<ConfigurationDeploymentReadModel> BuildConfigurationDeploymentQuery(Guid? organizationId, Guid? storeId, Guid? kioskId,
        Guid? configurationReleaseId, ConfigurationDeploymentProfile? profile, ConfigurationDeploymentReadStatus? status, bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds, IEnumerable<Guid> allowedStoreIds, IEnumerable<Guid> allowedKioskIds)
    {
        IQueryable<ConfigurationDeploymentReadModel> query = profile switch
        {
            ConfigurationDeploymentProfile.FullEdge => FullEdgeDeploymentQuery(),
            ConfigurationDeploymentProfile.LowCostController => LowCostDeploymentQuery(),
            _ => FullEdgeDeploymentQuery().Concat(LowCostDeploymentQuery())
        };
        if (!isSystemAdmin)
        {
            var organizations = allowedOrganizationIds.ToArray(); var stores = allowedStoreIds.ToArray(); var kiosks = allowedKioskIds.ToArray();
            query = query.Where(deployment => organizations.Contains(deployment.OrganizationId) || stores.Contains(deployment.StoreId) || kiosks.Contains(deployment.KioskId));
        }
        if (organizationId.HasValue) query = query.Where(deployment => deployment.OrganizationId == organizationId.Value);
        if (storeId.HasValue) query = query.Where(deployment => deployment.StoreId == storeId.Value);
        if (kioskId.HasValue) query = query.Where(deployment => deployment.KioskId == kioskId.Value);
        if (configurationReleaseId.HasValue) query = query.Where(deployment => deployment.ConfigurationReleaseId == configurationReleaseId.Value);
        if (status.HasValue) query = query.Where(deployment => deployment.Status == status.Value);
        return query;
    }

    private IQueryable<ConfigurationDeploymentReadModel> FullEdgeDeploymentQuery() => _dbContext.KioskConfigurationDeployments.AsNoTracking()
        .Select(deployment => new ConfigurationDeploymentReadModel
        {
            Id = deployment.Id, Profile = ConfigurationDeploymentProfile.FullEdge, OrganizationId = deployment.KioskExecutionEndpoint.Kiosk.OrganizationId,
            StoreId = deployment.KioskExecutionEndpoint.Kiosk.StoreId, KioskId = deployment.KioskId, KioskExecutionEndpointId = deployment.KioskExecutionEndpointId,
            EndpointCode = deployment.KioskExecutionEndpoint.EndpointCode, ConfigurationReleaseId = deployment.ConfigurationReleaseId,
            ReleaseNumber = deployment.ConfigurationRelease.ReleaseNumber, ReleaseChecksum = deployment.ReleaseChecksum, Status = (ConfigurationDeploymentReadStatus)deployment.Status,
            RequestedAt = deployment.RequestedAt, RequestedByAccountId = deployment.RequestedByAccountId, ExecutorReportedAt = deployment.EdgeReportedAt,
            CloudReceivedAt = deployment.CloudReceivedAt, LastReportId = deployment.LastEdgeDeploymentEventId, FailureCode = deployment.FailureCode,
            FailureReason = deployment.FailureReason, AttemptNo = deployment.AttemptNo, EdgeRuntimeId = deployment.EdgeRuntimeId, ControllerId = null,
            ActiveSetVersion = null, ActiveSetChecksum = null, RequestedArtifactCount = null, RequestedArtifactStorageBytes = null,
            MaxArtifactCount = null, MaxArtifactStorageBytes = null
        });

    private IQueryable<ConfigurationDeploymentReadModel> LowCostDeploymentQuery() => _dbContext.ControllerArtifactSetDeployments.AsNoTracking()
        .Select(deployment => new ConfigurationDeploymentReadModel
        {
            Id = deployment.Id, Profile = ConfigurationDeploymentProfile.LowCostController, OrganizationId = deployment.KioskExecutionEndpoint.Kiosk.OrganizationId,
            StoreId = deployment.KioskExecutionEndpoint.Kiosk.StoreId, KioskId = deployment.KioskId, KioskExecutionEndpointId = deployment.KioskExecutionEndpointId,
            EndpointCode = deployment.KioskExecutionEndpoint.EndpointCode, ConfigurationReleaseId = deployment.SourceConfigurationReleaseId,
            ReleaseNumber = deployment.SourceConfigurationRelease.ReleaseNumber, ReleaseChecksum = deployment.ReleaseChecksum, Status = (ConfigurationDeploymentReadStatus)deployment.Status,
            RequestedAt = deployment.RequestedAt, RequestedByAccountId = deployment.RequestedByAccountId, ExecutorReportedAt = deployment.ControllerReportedAt,
            CloudReceivedAt = deployment.CloudReceivedAt, LastReportId = deployment.LastControllerReportId, FailureCode = deployment.FailureCode,
            FailureReason = deployment.FailureReason, AttemptNo = null, EdgeRuntimeId = null, ControllerId = deployment.ControllerId,
            ActiveSetVersion = deployment.ActiveSetVersion, ActiveSetChecksum = deployment.ActiveSetChecksum, RequestedArtifactCount = deployment.RequestedArtifactCount,
            RequestedArtifactStorageBytes = deployment.RequestedArtifactStorageBytes, MaxArtifactCount = deployment.MaxArtifactCount,
            MaxArtifactStorageBytes = deployment.MaxArtifactStorageBytes
        });
}
