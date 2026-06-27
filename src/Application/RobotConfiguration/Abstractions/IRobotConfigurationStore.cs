using Domain.RobotConfiguration.Entities;
using Domain.Tenants.Enums;
using Domain.RobotConfiguration.Enums;

namespace Application.RobotConfiguration.Abstractions;

public interface IRobotConfigurationStore
{
    Task<RobotArtifact?> GetArtifactForPublishAsync(Guid artifactId, CancellationToken cancellationToken = default);

    Task<RobotProgram?> GetProgramForPublishAsync(Guid programId, CancellationToken cancellationToken = default);

    Task<RobotProgram?> GetProgramForEditAsync(Guid programId, CancellationToken cancellationToken = default);

    Task<RobotArtifact?> GetArtifactByIdAsync(Guid artifactId, CancellationToken cancellationToken = default);

    Task<RobotProgram?> GetProgramByIdAsync(Guid programId, CancellationToken cancellationToken = default);

    Task<int> CountArtifactsAsync(Guid organizationId, string? search, RobotArtifactStatus? status, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RobotArtifact>> ListArtifactsAsync(
        Guid organizationId,
        string? search,
        RobotArtifactStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountProgramsAsync(
        Guid? organizationId,
        string? search,
        RobotProgramStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds,
        IEnumerable<Guid> allowedStoreIds,
        IEnumerable<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RobotProgram>> ListProgramsAsync(
        Guid? organizationId,
        string? search,
        RobotProgramStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds,
        IEnumerable<Guid> allowedStoreIds,
        IEnumerable<Guid> allowedKioskIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken = default);

    Task<bool> ArtifactExistsAsync(
        Guid organizationId,
        string artifactCode,
        string checksum,
        CancellationToken cancellationToken = default);

    Task<bool> ProgramCodeExistsAsync(
        Guid organizationId,
        Guid? storeId,
        Guid? kioskId,
        Guid? deviceId,
        string code,
        Guid? excludeProgramId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ProgramScopeExistsAsync(
        TenantScopeType scopeType,
        Guid organizationId,
        Guid? storeId,
        Guid? kioskId,
        Guid? deviceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RobotArtifact>> ListArtifactsByIdsAsync(
        IReadOnlyCollection<Guid> artifactIds,
        CancellationToken cancellationToken = default);

    Task AddArtifactAsync(RobotArtifact artifact, CancellationToken cancellationToken = default);

    Task AddProgramAsync(RobotProgram program, CancellationToken cancellationToken = default);

    void DeleteProgramArtifacts(IEnumerable<RobotProgramArtifact> programArtifacts);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
