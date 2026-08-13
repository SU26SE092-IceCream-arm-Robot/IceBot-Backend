using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.AuthoringImports;
using Domain.RobotConfiguration.Programs;
using Application.RobotConfiguration.AuthoringImports.Queries;

namespace Application.RobotConfiguration.AuthoringImports;

public interface IRobotAuthoringImportStore
{
    Task<bool> ScopeExistsAsync(Guid organizationId, Guid? storeId, Guid? kioskId, Guid? deviceId, CancellationToken cancellationToken);
    Task<RobotAuthoringImport?> GetByIdempotencyKeyAsync(Guid organizationId, string idempotencyKey, bool tracked, CancellationToken cancellationToken);
    Task<RobotAuthoringImport?> GetAsync(Guid organizationId, Guid importId, bool tracked, CancellationToken cancellationToken);
    Task<int> CountImportsAsync(RobotAuthoringImportListCriteria criteria, CancellationToken cancellationToken);
    Task<IReadOnlyList<RobotAuthoringImportListRow>> ListImportsAsync(RobotAuthoringImportListCriteria criteria, CancellationToken cancellationToken);
    Task<IReadOnlyList<RobotArtifactTechnicalContract>> GetContractsAsync(Guid organizationId, IReadOnlyCollection<string> codes, bool tracked, CancellationToken cancellationToken);
    Task<IReadOnlyList<RobotArtifact>> GetArtifactsAsync(Guid organizationId, IReadOnlyCollection<string> codes, bool tracked, CancellationToken cancellationToken);
    Task<RobotProgram?> GetProgramAsync(Guid organizationId, Guid? storeId, Guid? kioskId, Guid? deviceId, string code, bool tracked, CancellationToken cancellationToken);
    Task<RobotAuthoringImport?> BeginMutationAsync(Guid organizationId, Guid importId, CancellationToken cancellationToken);
    Task LockMaterializationResourceIdentitiesAsync(Guid organizationId, Guid? storeId, Guid? kioskId, Guid? deviceId, string programCode, IReadOnlyCollection<string> artifactCodes, CancellationToken cancellationToken);
    Task CommitMutationAsync(CancellationToken cancellationToken);
    Task RollbackMutationAsync(CancellationToken cancellationToken);
    Task<(bool Created, RobotAuthoringImport Import)> InsertOrGetExistingAsync(RobotAuthoringImport importSession, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task PrepareMaterializationAsync(IReadOnlyCollection<RobotArtifactTechnicalContract> contracts, IReadOnlyCollection<RobotArtifact> artifacts, RobotProgram? program, CancellationToken cancellationToken);
    Task CommitPreparedMutationAsync(CancellationToken cancellationToken);
}
