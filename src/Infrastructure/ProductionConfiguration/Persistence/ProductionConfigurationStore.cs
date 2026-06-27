using Application.ProductionConfiguration.Abstractions;
using Domain.Devices.Entities;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Catalog.Entities;
using Domain.RobotConfiguration.Entities;

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
}
