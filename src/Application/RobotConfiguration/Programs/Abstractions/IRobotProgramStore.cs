using Application.RobotConfiguration.Programs.ReadModels;
using Domain.RobotConfiguration.Programs;
using Domain.Tenants.Enums;

namespace Application.RobotConfiguration.Programs.Abstractions;

public interface IRobotProgramStore
{
    Task<RobotProgram?> GetProgramForPublishAsync(Guid programId, CancellationToken cancellationToken = default);

    Task<RobotProgram?> GetProgramForEditAsync(Guid programId, CancellationToken cancellationToken = default);

    Task<RobotProgram?> GetProgramByIdAsync(Guid programId, CancellationToken cancellationToken = default);

    Task<int> CountProgramsAsync(
        Guid? organizationId,
        string? search,
        RobotProgramStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds,
        IEnumerable<Guid> allowedStoreIds,
        IEnumerable<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RobotProgramSummaryReadModel>> ListProgramsAsync(
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

    Task<bool> ProgramIsReferencedByDraftReleaseAsync(
        Guid programId,
        CancellationToken cancellationToken = default);

    Task<RobotProgramDiscardOutcome> DiscardDraftProgramAsync(
        RobotProgram program,
        CancellationToken cancellationToken = default);

    Task AddProgramAsync(RobotProgram program, CancellationToken cancellationToken = default);

    Task SaveProgramReplacementAsync(
        IReadOnlyCollection<RobotProgramArtifact> removedArtifacts,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public enum RobotProgramDiscardOutcome
{
    Deleted = 1,
    Referenced = 2
}
