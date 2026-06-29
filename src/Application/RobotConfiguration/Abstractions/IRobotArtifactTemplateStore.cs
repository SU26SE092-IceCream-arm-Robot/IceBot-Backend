using Domain.RobotConfiguration.Entities;
using Domain.RobotConfiguration.Enums;

namespace Application.RobotConfiguration.Abstractions;

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

    Task<IReadOnlyCollection<string>> ListStorageKeysAsync(CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed record RobotArtifactTemplateInsertResult(bool Created, RobotArtifactTemplate Template);
