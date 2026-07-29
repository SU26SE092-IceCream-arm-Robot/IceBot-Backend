using Application.Identity.Tokens.Claims;
using Application.ProductionConfiguration.Deployments.ReadModels;

namespace Application.ProductionConfiguration.Deployments.Queries;

public sealed record GetConfigurationDeploymentArtifactsQuery(
    CurrentUserContext UserContext,
    Guid KioskId,
    Guid DeploymentId);

public sealed record ConfigurationDeploymentArtifactResult(
    Guid ExecutionRouteId,
    Guid RobotProgramId,
    Guid RobotArtifactId,
    int RunOrder,
    string ArtifactChecksum,
    long ContentLengthBytes,
    string RuntimeTargetCode,
    string MachineModelCode,
    string? RequiredOptionCode);

public interface IConfigurationDeploymentArtifactReader
{
    Task<IReadOnlyCollection<ConfigurationDeploymentArtifactResult>> ListAsync(
        ConfigurationDeploymentReadModel deployment,
        CancellationToken cancellationToken = default);
}
