using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.Programs.Manifests;

namespace Application.RobotConfiguration.Artifacts.Abstractions;

public interface IRobotArtifactStore
{
    Task<RobotArtifact?> GetArtifactForPublishAsync(
        Guid organizationId,
        Guid artifactId,
        CancellationToken cancellationToken = default);

    Task<RobotArtifact?> GetArtifactByIdAsync(
        Guid organizationId,
        Guid artifactId,
        CancellationToken cancellationToken = default);

    Task<int> CountArtifactsAsync(
        Guid organizationId,
        string? search,
        RobotArtifactStatus? status,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RobotArtifact>> ListArtifactsAsync(
        Guid organizationId,
        string? search,
        RobotArtifactStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task<RobotArtifact?> GetArtifactByCodeAndChecksumAsync(
        Guid organizationId,
        string artifactCode,
        string checksum,
        CancellationToken cancellationToken = default);

    Task<bool> ArtifactIsReferencedByDraftProgramAsync(
        Guid artifactId,
        CancellationToken cancellationToken = default);

    Task<RobotArtifactDiscardOutcome> DiscardDraftArtifactAsync(
        RobotArtifact artifact,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RobotArtifact>> ListArtifactsByIdsAsync(
        Guid organizationId,
        IReadOnlyCollection<Guid> artifactIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RobotArtifactManifestSnapshot>> ListArtifactManifestSnapshotsAsync(
        Guid organizationId,
        IReadOnlyCollection<Guid> artifactIds,
        CancellationToken cancellationToken = default);

    Task<RobotArtifactInsertResult> InsertArtifactOrGetExistingAsync(
        RobotArtifact artifact,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed record RobotArtifactInsertResult(bool Created, RobotArtifact Artifact);

public enum RobotArtifactDiscardOutcome
{
    Deleted = 1,
    Referenced = 2
}
