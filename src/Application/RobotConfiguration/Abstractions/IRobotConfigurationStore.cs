using Domain.RobotConfiguration.Entities;

namespace Application.RobotConfiguration.Abstractions;

public interface IRobotConfigurationStore
{
    Task<RobotProgram?> GetProgramForPublishAsync(Guid programId, CancellationToken cancellationToken = default);

    Task AddArtifactAsync(RobotArtifact artifact, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
