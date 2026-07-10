using Application.RobotConfiguration.Artifacts.Abstractions;
using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.Programs;
using Domain.RobotConfiguration.Programs.Manifests;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;

namespace Infrastructure.RobotConfiguration.Artifacts.Persistence;

public sealed class RobotArtifactStore : IRobotArtifactStore
{
    private readonly IceBotDbContext _dbContext;

    public RobotArtifactStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RobotArtifact?> GetArtifactForPublishAsync(
        Guid organizationId,
        Guid artifactId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.RobotArtifacts
            .FirstOrDefaultAsync(
                artifact => artifact.Id == artifactId && artifact.OrganizationId == organizationId,
                cancellationToken);
    }

    public Task<RobotArtifact?> GetArtifactByIdAsync(
        Guid organizationId,
        Guid artifactId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.RobotArtifacts.AsNoTracking()
            .FirstOrDefaultAsync(
                artifact => artifact.Id == artifactId && artifact.OrganizationId == organizationId,
                cancellationToken);
    }

    public Task<int> CountArtifactsAsync(
        Guid organizationId,
        string? search,
        RobotArtifactStatus? status,
        CancellationToken cancellationToken = default)
    {
        return BuildArtifactQuery(organizationId, search, status).CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RobotArtifact>> ListArtifactsAsync(
        Guid organizationId,
        string? search,
        RobotArtifactStatus? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await BuildArtifactQuery(organizationId, search, status)
            .OrderBy(artifact => artifact.ArtifactCode)
            .ThenByDescending(artifact => artifact.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Organizations.AnyAsync(
            organization => organization.Id == organizationId && organization.DeletedAt == null,
            cancellationToken);
    }

    public Task<RobotArtifact?> GetArtifactByCodeAndChecksumAsync(
        Guid organizationId,
        string artifactCode,
        string checksum,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.RobotArtifacts.AsNoTracking().FirstOrDefaultAsync(
            artifact => artifact.OrganizationId == organizationId &&
                artifact.ArtifactCode == artifactCode &&
                artifact.Checksum == checksum &&
                artifact.DeletedAt == null,
            cancellationToken);
    }

    public Task<bool> ArtifactIsReferencedByDraftProgramAsync(
        Guid artifactId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.RobotProgramArtifacts.AnyAsync(
            item => item.RobotArtifactId == artifactId &&
                item.RobotProgram.Status == RobotProgramStatus.Draft,
            cancellationToken);
    }

    public async Task<RobotArtifactDiscardOutcome> DiscardDraftArtifactAsync(
        RobotArtifact artifact,
        CancellationToken cancellationToken = default)
    {
        if (await _dbContext.RobotProgramArtifacts.AnyAsync(
                item => item.RobotArtifactId == artifact.Id,
                cancellationToken))
        {
            return RobotArtifactDiscardOutcome.Referenced;
        }

        EntityEntry<RobotArtifact> entry = _dbContext.RobotArtifacts.Remove(artifact);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return RobotArtifactDiscardOutcome.Deleted;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            entry.State = EntityState.Unchanged;
            return RobotArtifactDiscardOutcome.Referenced;
        }
    }

    public async Task<IReadOnlyList<RobotArtifact>> ListArtifactsByIdsAsync(
        Guid organizationId,
        IReadOnlyCollection<Guid> artifactIds,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.RobotArtifacts
            .Where(artifact => artifact.OrganizationId == organizationId && artifactIds.Contains(artifact.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RobotArtifactManifestSnapshot>> ListArtifactManifestSnapshotsAsync(
        Guid organizationId,
        IReadOnlyCollection<Guid> artifactIds,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.RobotArtifacts.AsNoTracking()
            .Where(artifact => artifact.OrganizationId == organizationId && artifactIds.Contains(artifact.Id))
            .Select(artifact => new RobotArtifactManifestSnapshot(
                artifact.Id,
                artifact.ArtifactCode,
                artifact.ArtifactName,
                artifact.FileName,
                artifact.Status,
                artifact.Checksum,
                artifact.StorageKey,
                artifact.RuntimeTargetCode,
                artifact.MachineModelCode,
                artifact.ContentLengthBytes))
            .ToListAsync(cancellationToken);
    }

    public async Task<RobotArtifactInsertResult> InsertArtifactOrGetExistingAsync(
        RobotArtifact artifact,
        CancellationToken cancellationToken = default)
    {
        EntityEntry<RobotArtifact> entry = await _dbContext.RobotArtifacts.AddAsync(artifact, cancellationToken);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return new RobotArtifactInsertResult(true, artifact);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            entry.State = EntityState.Detached;
            var existing = await _dbContext.RobotArtifacts.AsNoTracking().FirstOrDefaultAsync(
                candidate => candidate.OrganizationId == artifact.OrganizationId &&
                    candidate.ArtifactCode == artifact.ArtifactCode &&
                    candidate.Checksum == artifact.Checksum &&
                    candidate.DeletedAt == null,
                cancellationToken);

            if (existing is null)
                throw;

            return new RobotArtifactInsertResult(false, existing);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<RobotArtifact> BuildArtifactQuery(
        Guid organizationId,
        string? search,
        RobotArtifactStatus? status)
    {
        var query = _dbContext.RobotArtifacts.AsNoTracking()
            .Where(artifact => artifact.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(artifact =>
                EF.Functions.ILike(artifact.ArtifactCode, $"%{term}%") ||
                EF.Functions.ILike(artifact.ArtifactName, $"%{term}%") ||
                EF.Functions.ILike(artifact.FileName, $"%{term}%"));
        }

        if (status.HasValue)
        {
            query = query.Where(artifact => artifact.Status == status.Value);
        }

        return query;
    }
}
