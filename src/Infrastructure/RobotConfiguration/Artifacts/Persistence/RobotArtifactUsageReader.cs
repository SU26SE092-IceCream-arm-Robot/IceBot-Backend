using Application.RobotConfiguration.Artifacts.Queries;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.RobotConfiguration.Artifacts.Persistence;

public sealed class RobotArtifactUsageReader(IceBotDbContext db) : IRobotArtifactUsageReader
{
    public async Task<RobotArtifactUsageResult?> GetAsync(
        Guid organizationId,
        Guid artifactId,
        CancellationToken cancellationToken = default)
    {
        var exists = await db.RobotArtifacts.AsNoTracking().AnyAsync(
            artifact => artifact.Id == artifactId && artifact.OrganizationId == organizationId,
            cancellationToken);
        if (!exists) return null;

        var programRows = await db.RobotProgramArtifacts.AsNoTracking()
            .Where(item => item.RobotArtifactId == artifactId &&
                item.RobotProgram.OrganizationId == organizationId)
            .Select(item => new
            {
                item.RobotProgramId,
                item.RobotProgram.Code,
                item.RobotProgram.Name,
                item.RobotProgram.Status,
                item.RunOrder
            })
            .OrderBy(item => item.Code)
            .ThenBy(item => item.RunOrder)
            .ToArrayAsync(cancellationToken);
        var programs = programRows.Select(item => new RobotArtifactUsageProgramResult(
            item.RobotProgramId, item.Code, item.Name, item.Status.ToString(), item.RunOrder)).ToArray();

        var programIds = programs.Select(item => item.RobotProgramId).Distinct().ToArray();
        var releaseRows = programIds.Length == 0
            ? []
            : await db.ExecutionRouteRobotBindings.AsNoTracking()
                .Where(binding => programIds.Contains(binding.RobotProgramId) &&
                    binding.ExecutionRoute.ConfigurationRelease.OrganizationId == organizationId)
                .Select(binding => new
                {
                    binding.ExecutionRoute.ConfigurationReleaseId,
                    binding.ExecutionRoute.ConfigurationRelease.ReleaseNumber,
                    binding.ExecutionRoute.ConfigurationRelease.Status,
                    binding.ExecutionRouteId,
                    binding.ExecutionRoute.RouteCode,
                    binding.RobotProgramId
                })
                .OrderByDescending(item => item.ReleaseNumber)
                .ThenBy(item => item.RouteCode)
                .ToArrayAsync(cancellationToken);
        var releases = releaseRows.Select(item => new RobotArtifactUsageReleaseResult(
            item.ConfigurationReleaseId, item.ReleaseNumber, item.Status.ToString(), item.ExecutionRouteId,
            item.RouteCode, item.RobotProgramId)).ToArray();

        var releaseIds = releases.Select(item => item.ConfigurationReleaseId).Distinct().ToArray();
        var fullEdgeRows = releaseIds.Length == 0
            ? []
            : await db.KioskConfigurationDeployments.AsNoTracking()
                .Where(deployment => deployment.OrganizationId == organizationId &&
                    releaseIds.Contains(deployment.ConfigurationReleaseId))
                .Select(deployment => new
                {
                    deployment.Id,
                    deployment.KioskId,
                    deployment.ConfigurationReleaseId,
                    deployment.Status
                })
                .ToArrayAsync(cancellationToken);
        var fullEdgeDeployments = fullEdgeRows.Select(deployment => new RobotArtifactUsageDeploymentResult(
            deployment.Id, "FullEdge", deployment.KioskId, deployment.ConfigurationReleaseId,
            deployment.Status.ToString())).ToArray();

        var lowCostRows = await db.ControllerArtifactSetItems.AsNoTracking()
            .Where(item => item.RobotArtifactId == artifactId &&
                item.ControllerArtifactSetDeployment.OrganizationId == organizationId)
            .Select(item => new
            {
                DeploymentId = item.ControllerArtifactSetDeploymentId,
                item.ControllerArtifactSetDeployment.KioskId,
                ConfigurationReleaseId = item.ControllerArtifactSetDeployment.SourceConfigurationReleaseId,
                item.ControllerArtifactSetDeployment.Status
            })
            .Distinct()
            .ToArrayAsync(cancellationToken);
        var lowCostDeployments = lowCostRows.Select(deployment => new RobotArtifactUsageDeploymentResult(
            deployment.DeploymentId, "LowCostController", deployment.KioskId,
            deployment.ConfigurationReleaseId, deployment.Status.ToString())).ToArray();

        return new RobotArtifactUsageResult(
            artifactId,
            programs,
            releases,
            fullEdgeDeployments.Concat(lowCostDeployments)
                .OrderBy(item => item.KioskId)
                .ThenBy(item => item.DeploymentId)
                .ToArray());
    }
}
