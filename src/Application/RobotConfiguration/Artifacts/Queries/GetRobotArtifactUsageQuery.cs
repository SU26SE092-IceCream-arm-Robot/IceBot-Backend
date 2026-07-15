using Application.Identity.Tokens.Claims;

namespace Application.RobotConfiguration.Artifacts.Queries;

public sealed record GetRobotArtifactUsageQuery(
    CurrentUserContext UserContext,
    Guid OrganizationId,
    Guid ArtifactId);

public sealed record RobotArtifactUsageProgramResult(
    Guid RobotProgramId,
    string ProgramCode,
    string ProgramName,
    string Status,
    int RunOrder);

public sealed record RobotArtifactUsageReleaseResult(
    Guid ConfigurationReleaseId,
    long ReleaseNumber,
    string Status,
    Guid ExecutionRouteId,
    string RouteCode,
    Guid RobotProgramId);

public sealed record RobotArtifactUsageDeploymentResult(
    Guid DeploymentId,
    string Profile,
    Guid KioskId,
    Guid ConfigurationReleaseId,
    string Status);

public sealed record RobotArtifactUsageResult(
    Guid RobotArtifactId,
    IReadOnlyCollection<RobotArtifactUsageProgramResult> Programs,
    IReadOnlyCollection<RobotArtifactUsageReleaseResult> Releases,
    IReadOnlyCollection<RobotArtifactUsageDeploymentResult> Deployments);

public interface IRobotArtifactUsageReader
{
    Task<RobotArtifactUsageResult?> GetAsync(
        Guid organizationId,
        Guid artifactId,
        CancellationToken cancellationToken = default);
}
