using Application.ProductionConfiguration.Deployments.Queries;
using Application.ProductionConfiguration.Deployments.ReadModels;
using Domain.RobotConfiguration.Programs.Manifests;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ProductionConfiguration.Persistence.Deployments;

public sealed class ConfigurationDeploymentArtifactReader(IceBotDbContext db)
    : IConfigurationDeploymentArtifactReader
{
    public async Task<IReadOnlyCollection<ConfigurationDeploymentArtifactResult>> ListAsync(
        ConfigurationDeploymentReadModel deployment,
        CancellationToken cancellationToken = default)
    {
        if (deployment.Profile == ConfigurationDeploymentProfile.LowCostController)
        {
            return await db.ControllerArtifactSetItems.AsNoTracking()
                .Where(item => item.ControllerArtifactSetDeploymentId == deployment.Id)
                .OrderBy(item => item.ExecutionRouteId)
                .ThenBy(item => item.RobotProgramId)
                .ThenBy(item => item.RunOrder)
                .Select(item => new ConfigurationDeploymentArtifactResult(
                    item.ExecutionRouteId,
                    item.RobotProgramId,
                    item.RobotArtifactId,
                    item.RunOrder,
                    item.ArtifactChecksum,
                    item.ContentLengthBytes,
                    item.RuntimeTargetCode,
                    item.MachineModelCode,
                    item.RequiredOptionCode))
                .ToArrayAsync(cancellationToken);
        }

        var programs = await db.ExecutionRouteRobotBindings.AsNoTracking()
            .Where(binding => binding.ExecutionRoute.ConfigurationReleaseId == deployment.ConfigurationReleaseId)
            .Select(binding => new
            {
                binding.ExecutionRouteId,
                binding.RobotProgramId,
                binding.RobotProgram.ProgramManifestJson
            })
            .OrderBy(item => item.ExecutionRouteId)
            .ThenBy(item => item.RobotProgramId)
            .ToArrayAsync(cancellationToken);

        return programs.SelectMany(program =>
            RobotProgramManifestBuilder.Parse(
                    program.ProgramManifestJson ?? throw new InvalidOperationException(
                        $"Published robot program '{program.RobotProgramId}' is missing its manifest."))
                .Artifacts.Select(item => new ConfigurationDeploymentArtifactResult(
                    program.ExecutionRouteId,
                    program.RobotProgramId,
                    item.RobotArtifact.Id,
                    item.RunOrder,
                    item.RobotArtifact.Checksum,
                    item.RobotArtifact.ContentLengthBytes,
                    item.RobotArtifact.RuntimeTargetCode,
                    item.RobotArtifact.MachineModelCode,
                    item.RequiredOptionCode)))
            .OrderBy(item => item.ExecutionRouteId)
            .ThenBy(item => item.RobotProgramId)
            .ThenBy(item => item.RunOrder)
            .ToArray();
    }
}
