using Domain.RobotConfiguration.Entities;

namespace Application.RobotConfiguration.Abstractions;

public interface IRobotConfigurationStore
{
    Task<RobotArtifact?> GetArtifactForPublishAsync(Guid artifactId, CancellationToken cancellationToken = default);

    Task<RobotProgram?> GetProgramForPublishAsync(Guid programId, CancellationToken cancellationToken = default);

    Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task<bool> ArtifactExistsAsync(
        Guid organizationId,
        string artifactCode,
        string checksum,
        CancellationToken cancellationToken = default);

    Task AddArtifactAsync(RobotArtifact artifact, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
