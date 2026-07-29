using Application.RobotConfiguration.Artifacts.Results;
using Application.RobotConfiguration.Artifacts.Queries;
using Application.RobotConfiguration.Artifacts.Commands;
using Application.RobotConfiguration.ArtifactTemplates.Results;
using Application.RobotConfiguration.ArtifactTemplates.Queries;
using Application.RobotConfiguration.ArtifactTemplates.Commands;
using Domain.RobotConfiguration.ArtifactTemplates;
using Domain.RobotConfiguration.Artifacts;

namespace Application.RobotConfiguration.ArtifactTemplates.Abstractions;

public interface IRobotArtifactTemplateStore
{
    Task<RobotArtifactTemplate?> GetByIdAsync(
        Guid templateId,
        bool tracked = false,
        CancellationToken cancellationToken = default);

    Task<RobotArtifactTemplate?> GetByCodeAndChecksumAsync(
        string templateCode,
        string checksum,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        string? search,
        RobotArtifactStatus? status,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RobotArtifactTemplate>> ListAsync(
        string? search,
        RobotArtifactStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<RobotArtifactTemplateInsertResult> InsertOrGetExistingAsync(
        RobotArtifactTemplate template,
        CancellationToken cancellationToken = default);

    Task<RobotArtifactTemplateDiscardOutcome> DiscardDraftAsync(
        RobotArtifactTemplate template,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed record RobotArtifactTemplateInsertResult(bool Created, RobotArtifactTemplate Template);

public enum RobotArtifactTemplateDiscardOutcome
{
    Deleted = 1,
    Referenced = 2
}
