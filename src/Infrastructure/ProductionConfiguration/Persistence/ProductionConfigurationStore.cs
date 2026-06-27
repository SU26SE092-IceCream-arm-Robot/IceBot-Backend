using Application.ProductionConfiguration.Abstractions;
using Domain.Devices.Entities;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Catalog.Entities;
using Domain.RobotConfiguration.Entities;
using Application.ProductionConfiguration.ReadModels;

namespace Infrastructure.ProductionConfiguration.Persistence;

public sealed class ProductionConfigurationStore : IProductionConfigurationStore
{
    private readonly IceBotDbContext _dbContext;

    public ProductionConfigurationStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ConfigurationRelease?> GetReleaseForPublishAsync(Guid releaseId, CancellationToken cancellationToken = default)
    {
        return ReleaseGraph()
            .FirstOrDefaultAsync(release => release.Id == releaseId, cancellationToken);
    }

    public Task<ConfigurationRelease?> GetPublishedReleaseForDeploymentAsync(Guid releaseId, CancellationToken cancellationToken = default)
    {
        return ReleaseGraph()
            .FirstOrDefaultAsync(release => release.Id == releaseId, cancellationToken);
    }

    public Task<ConfigurationRelease?> GetReleaseByIdAsync(Guid releaseId, CancellationToken cancellationToken = default)
    {
        return ReleaseGraph(asNoTracking: true)
            .FirstOrDefaultAsync(release => release.Id == releaseId, cancellationToken);
    }

    public Task<ConfigurationRelease?> GetReleaseForEditAsync(Guid releaseId, CancellationToken cancellationToken = default)
    {
        return ReleaseGraph()
            .FirstOrDefaultAsync(release => release.Id == releaseId, cancellationToken);
    }

    public Task<KioskConfigurationDeployment?> GetFullEdgeDeploymentForReconciliationAsync(
        Guid deploymentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskConfigurationDeployments
            .FirstOrDefaultAsync(deployment => deployment.Id == deploymentId, cancellationToken);
    }

    public Task<ControllerArtifactSetDeployment?> GetControllerDeploymentForReconciliationAsync(
        Guid deploymentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ControllerArtifactSetDeployments
            .FirstOrDefaultAsync(deployment => deployment.Id == deploymentId, cancellationToken);
    }

    public Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Organizations.AnyAsync(
            organization => organization.Id == organizationId && organization.DeletedAt == null,
            cancellationToken);
    }

    public async Task<long> GetNextReleaseNumberAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var maximum = await _dbContext.ConfigurationReleases
            .Where(release => release.OrganizationId == organizationId)
            .Select(release => (long?)release.ReleaseNumber)
            .MaxAsync(cancellationToken);
        return (maximum ?? 0) + 1;
    }

    public Task<int> CountReleasesAsync(
        Guid? organizationId,
        ConfigurationReleaseStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds,
        CancellationToken cancellationToken = default)
    {
        return BuildReleaseListQuery(organizationId, status, isSystemAdmin, allowedOrganizationIds)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConfigurationRelease>> ListReleasesAsync(
        Guid? organizationId,
        ConfigurationReleaseStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await BuildReleaseListQuery(organizationId, status, isSystemAdmin, allowedOrganizationIds)
            .Include(release => release.ExecutionRoutes)
                .ThenInclude(route => route.ProductVariant)
            .Include(release => release.ExecutionRoutes)
                .ThenInclude(route => route.Recipe)
            .Include(release => release.ExecutionRoutes)
                .ThenInclude(route => route.RobotBindings)
                    .ThenInclude(binding => binding.RobotProgram)
            .OrderByDescending(release => release.ReleaseNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductVariant>> ListProductVariantsByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ProductVariants.AsNoTracking()
            .Include(variant => variant.Product)
            .Where(variant => ids.Contains(variant.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Recipe>> ListRecipesByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Recipes.AsNoTracking()
            .Where(recipe => ids.Contains(recipe.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RobotProgram>> ListRobotProgramsByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.RobotPrograms.AsNoTracking()
            .Where(program => ids.Contains(program.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountConfigurationDeploymentsAsync(
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
        CancellationToken cancellationToken = default)
    {
        return BuildConfigurationDeploymentQuery(
            organizationId, storeId, kioskId, configurationReleaseId, profile, status,
            isSystemAdmin, allowedOrganizationIds, allowedStoreIds, allowedKioskIds)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ConfigurationDeploymentReadModel>> ListConfigurationDeploymentsAsync(
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
        CancellationToken cancellationToken = default)
    {
        return await BuildConfigurationDeploymentQuery(
                organizationId, storeId, kioskId, configurationReleaseId, profile, status,
                isSystemAdmin, allowedOrganizationIds, allowedStoreIds, allowedKioskIds)
            .OrderByDescending(deployment => deployment.RequestedAt)
            .ThenByDescending(deployment => deployment.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<ConfigurationDeploymentReadModel?> GetConfigurationDeploymentAsync(
        Guid deploymentId,
        CancellationToken cancellationToken = default)
    {
        return FullEdgeDeploymentQuery().Where(deployment => deployment.Id == deploymentId)
            .Concat(LowCostDeploymentQuery().Where(deployment => deployment.Id == deploymentId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<ControllerArtifactSetDeployment?> GetControllerArtifactSetDeploymentForRollbackAsync(
        Guid deploymentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ControllerArtifactSetDeployments.AsNoTracking()
            .Include(deployment => deployment.Items)
            .FirstOrDefaultAsync(deployment => deployment.Id == deploymentId, cancellationToken);
    }

    public Task<KioskExecutionEndpoint?> GetEndpointForDeploymentAsync(Guid endpointId, CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskExecutionEndpoints
            .Include(endpoint => endpoint.Kiosk)
            .Include(endpoint => endpoint.CredentialBinding)
            .Include(endpoint => endpoint.SupportedRobotTargets)
                .ThenInclude(target => target.Device)
            .FirstOrDefaultAsync(endpoint => endpoint.Id == endpointId, cancellationToken);
    }

    public Task<bool> HasPendingFullEdgeDeploymentAsync(Guid kioskId, CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskConfigurationDeployments.AnyAsync(
            deployment => deployment.KioskId == kioskId &&
                (deployment.Status == KioskConfigurationDeploymentStatus.Pending ||
                    deployment.Status == KioskConfigurationDeploymentStatus.Installed),
            cancellationToken);
    }

    public async Task<int> GetNextFullEdgeDeploymentAttemptNoAsync(
        Guid kioskId,
        Guid configurationReleaseId,
        CancellationToken cancellationToken = default)
    {
        var maxAttempt = await _dbContext.KioskConfigurationDeployments
            .Where(deployment => deployment.KioskId == kioskId && deployment.ConfigurationReleaseId == configurationReleaseId)
            .Select(deployment => (int?)deployment.AttemptNo)
            .MaxAsync(cancellationToken);

        return (maxAttempt ?? 0) + 1;
    }

    public Task<bool> HasPendingControllerArtifactSetDeploymentAsync(Guid controllerId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ControllerArtifactSetDeployments.AnyAsync(
            deployment => deployment.ControllerId == controllerId &&
                (deployment.Status == ControllerArtifactSetDeploymentStatus.Pending ||
                    deployment.Status == ControllerArtifactSetDeploymentStatus.Installed),
            cancellationToken);
    }

    public async Task<bool> ReleaseHasPendingDeploymentAsync(
        Guid releaseId,
        CancellationToken cancellationToken = default)
    {
        var hasFullEdge = await _dbContext.KioskConfigurationDeployments.AnyAsync(
            deployment => deployment.ConfigurationReleaseId == releaseId &&
                (deployment.Status == KioskConfigurationDeploymentStatus.Pending ||
                    deployment.Status == KioskConfigurationDeploymentStatus.Installed),
            cancellationToken);
        if (hasFullEdge)
            return true;

        return await _dbContext.ControllerArtifactSetDeployments.AnyAsync(
            deployment => deployment.SourceConfigurationReleaseId == releaseId &&
                (deployment.Status == ControllerArtifactSetDeploymentStatus.Pending ||
                    deployment.Status == ControllerArtifactSetDeploymentStatus.Installed),
            cancellationToken);
    }

    public async Task<long> GetNextControllerActiveSetVersionAsync(Guid controllerId, CancellationToken cancellationToken = default)
    {
        var maxVersion = await _dbContext.ControllerArtifactSetDeployments
            .Where(deployment => deployment.ControllerId == controllerId)
            .Select(deployment => (long?)deployment.ActiveSetVersion)
            .MaxAsync(cancellationToken);

        return (maxVersion ?? 0) + 1;
    }

    public Task AddFullEdgeDeploymentAsync(KioskConfigurationDeployment deployment, CancellationToken cancellationToken = default)
    {
        return _dbContext.KioskConfigurationDeployments.AddAsync(deployment, cancellationToken).AsTask();
    }

    public Task AddControllerArtifactSetDeploymentAsync(ControllerArtifactSetDeployment deployment, CancellationToken cancellationToken = default)
    {
        return _dbContext.ControllerArtifactSetDeployments.AddAsync(deployment, cancellationToken).AsTask();
    }

    public Task AddReleaseAsync(ConfigurationRelease release, CancellationToken cancellationToken = default)
    {
        return _dbContext.ConfigurationReleases.AddAsync(release, cancellationToken).AsTask();
    }

    public void DeleteReleaseRoutes(IEnumerable<ExecutionRoute> routes)
    {
        var routeArray = routes.ToArray();
        _dbContext.ExecutionRouteRobotBindings.RemoveRange(routeArray.SelectMany(route => route.RobotBindings));
        _dbContext.ExecutionRoutes.RemoveRange(routeArray);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<ConfigurationRelease> ReleaseGraph(bool asNoTracking = false)
    {
        var query = _dbContext.ConfigurationReleases.AsQueryable();
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query
            .Include(release => release.ExecutionRoutes)
                .ThenInclude(route => route.ProductVariant)
                    .ThenInclude(productVariant => productVariant.Product)
            .Include(release => release.ExecutionRoutes)
                .ThenInclude(route => route.Recipe)
            .Include(release => release.ExecutionRoutes)
                .ThenInclude(route => route.RobotBindings)
                    .ThenInclude(binding => binding.RobotProgram)
                        .ThenInclude(program => program.RobotProgramArtifacts)
                            .ThenInclude(programArtifact => programArtifact.RobotArtifact);
    }

    private IQueryable<ConfigurationRelease> BuildReleaseListQuery(
        Guid? organizationId,
        ConfigurationReleaseStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds)
    {
        var query = _dbContext.ConfigurationReleases.AsNoTracking();
        if (!isSystemAdmin)
        {
            var organizationIds = allowedOrganizationIds.ToArray();
            query = query.Where(release => organizationIds.Contains(release.OrganizationId));
        }

        if (organizationId.HasValue)
        {
            query = query.Where(release => release.OrganizationId == organizationId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(release => release.Status == status.Value);
        }

        return query;
    }

    private IQueryable<ConfigurationDeploymentReadModel> BuildConfigurationDeploymentQuery(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        Guid? configurationReleaseId,
        ConfigurationDeploymentProfile? profile,
        ConfigurationDeploymentReadStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds,
        IEnumerable<Guid> allowedStoreIds,
        IEnumerable<Guid> allowedKioskIds)
    {
        IQueryable<ConfigurationDeploymentReadModel> query = profile switch
        {
            ConfigurationDeploymentProfile.FullEdge => FullEdgeDeploymentQuery(),
            ConfigurationDeploymentProfile.LowCostController => LowCostDeploymentQuery(),
            _ => FullEdgeDeploymentQuery().Concat(LowCostDeploymentQuery())
        };

        if (!isSystemAdmin)
        {
            var organizationIds = allowedOrganizationIds.ToArray();
            var storeIds = allowedStoreIds.ToArray();
            var kioskIds = allowedKioskIds.ToArray();
            query = query.Where(deployment =>
                organizationIds.Contains(deployment.OrganizationId) ||
                storeIds.Contains(deployment.StoreId) ||
                kioskIds.Contains(deployment.KioskId));
        }

        if (organizationId.HasValue) query = query.Where(deployment => deployment.OrganizationId == organizationId.Value);
        if (storeId.HasValue) query = query.Where(deployment => deployment.StoreId == storeId.Value);
        if (kioskId.HasValue) query = query.Where(deployment => deployment.KioskId == kioskId.Value);
        if (configurationReleaseId.HasValue) query = query.Where(deployment => deployment.ConfigurationReleaseId == configurationReleaseId.Value);
        if (status.HasValue) query = query.Where(deployment => deployment.Status == status.Value);
        return query;
    }

    private IQueryable<ConfigurationDeploymentReadModel> FullEdgeDeploymentQuery()
    {
        return _dbContext.KioskConfigurationDeployments.AsNoTracking()
            .Select(deployment => new ConfigurationDeploymentReadModel
            {
                Id = deployment.Id,
                Profile = ConfigurationDeploymentProfile.FullEdge,
                OrganizationId = deployment.KioskExecutionEndpoint.Kiosk.OrganizationId,
                StoreId = deployment.KioskExecutionEndpoint.Kiosk.StoreId,
                KioskId = deployment.KioskId,
                KioskExecutionEndpointId = deployment.KioskExecutionEndpointId,
                EndpointCode = deployment.KioskExecutionEndpoint.EndpointCode,
                ConfigurationReleaseId = deployment.ConfigurationReleaseId,
                ReleaseNumber = deployment.ConfigurationRelease.ReleaseNumber,
                ReleaseChecksum = deployment.ReleaseChecksum,
                Status = (ConfigurationDeploymentReadStatus)deployment.Status,
                RequestedAt = deployment.RequestedAt,
                RequestedByAccountId = deployment.RequestedByAccountId,
                ExecutorReportedAt = deployment.EdgeReportedAt,
                CloudReceivedAt = deployment.CloudReceivedAt,
                LastReportId = deployment.LastEdgeDeploymentEventId,
                FailureCode = deployment.FailureCode,
                FailureReason = deployment.FailureReason,
                AttemptNo = deployment.AttemptNo,
                EdgeRuntimeId = deployment.EdgeRuntimeId,
                ControllerId = null,
                ActiveSetVersion = null,
                ActiveSetChecksum = null,
                RequestedArtifactCount = null,
                RequestedArtifactStorageBytes = null,
                MaxArtifactCount = null,
                MaxArtifactStorageBytes = null
            });
    }

    private IQueryable<ConfigurationDeploymentReadModel> LowCostDeploymentQuery()
    {
        return _dbContext.ControllerArtifactSetDeployments.AsNoTracking()
            .Select(deployment => new ConfigurationDeploymentReadModel
            {
                Id = deployment.Id,
                Profile = ConfigurationDeploymentProfile.LowCostController,
                OrganizationId = deployment.KioskExecutionEndpoint.Kiosk.OrganizationId,
                StoreId = deployment.KioskExecutionEndpoint.Kiosk.StoreId,
                KioskId = deployment.KioskId,
                KioskExecutionEndpointId = deployment.KioskExecutionEndpointId,
                EndpointCode = deployment.KioskExecutionEndpoint.EndpointCode,
                ConfigurationReleaseId = deployment.SourceConfigurationReleaseId,
                ReleaseNumber = deployment.SourceConfigurationRelease.ReleaseNumber,
                ReleaseChecksum = deployment.ReleaseChecksum,
                Status = (ConfigurationDeploymentReadStatus)deployment.Status,
                RequestedAt = deployment.RequestedAt,
                RequestedByAccountId = deployment.RequestedByAccountId,
                ExecutorReportedAt = deployment.ControllerReportedAt,
                CloudReceivedAt = deployment.CloudReceivedAt,
                LastReportId = deployment.LastControllerReportId,
                FailureCode = deployment.FailureCode,
                FailureReason = deployment.FailureReason,
                AttemptNo = null,
                EdgeRuntimeId = null,
                ControllerId = deployment.ControllerId,
                ActiveSetVersion = deployment.ActiveSetVersion,
                ActiveSetChecksum = deployment.ActiveSetChecksum,
                RequestedArtifactCount = deployment.RequestedArtifactCount,
                RequestedArtifactStorageBytes = deployment.RequestedArtifactStorageBytes,
                MaxArtifactCount = deployment.MaxArtifactCount,
                MaxArtifactStorageBytes = deployment.MaxArtifactStorageBytes
            });
    }
}
