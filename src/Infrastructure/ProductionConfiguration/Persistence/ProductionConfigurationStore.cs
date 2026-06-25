using Application.ProductionConfiguration.Abstractions;
using Domain.Devices.Entities;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<ConfigurationRelease> ReleaseGraph()
    {
        return _dbContext.ConfigurationReleases
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
}
