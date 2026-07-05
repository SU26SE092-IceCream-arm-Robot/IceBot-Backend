using Application.RobotConfiguration.Abstractions;
using Domain.RobotConfiguration.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Tenants.Enums;
using Domain.RobotConfiguration.Enums;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;
using Application.RobotConfiguration.ReadModels;
using Domain.RobotConfiguration.Manifests;

namespace Infrastructure.RobotConfiguration.Persistence;

public sealed class RobotConfigurationStore : IRobotConfigurationStore
{
    private readonly IceBotDbContext _dbContext;

    public RobotConfigurationStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RobotArtifact?> GetArtifactForPublishAsync(Guid artifactId, CancellationToken cancellationToken = default)
    {
        return _dbContext.RobotArtifacts
            .FirstOrDefaultAsync(artifact => artifact.Id == artifactId, cancellationToken);
    }

    public Task<RobotProgram?> GetProgramForPublishAsync(Guid programId, CancellationToken cancellationToken = default)
    {
        return _dbContext.RobotPrograms
            .Include(program => program.RobotProgramArtifacts)
            .FirstOrDefaultAsync(program => program.Id == programId, cancellationToken);
    }

    public Task<RobotProgram?> GetProgramForEditAsync(Guid programId, CancellationToken cancellationToken = default)
    {
        return _dbContext.RobotPrograms
            .Include(program => program.RobotProgramArtifacts)
            .FirstOrDefaultAsync(program => program.Id == programId, cancellationToken);
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

    public Task<RobotProgram?> GetProgramByIdAsync(Guid programId, CancellationToken cancellationToken = default)
    {
        return _dbContext.RobotPrograms.AsNoTracking()
            .Include(program => program.RobotProgramArtifacts)
            .FirstOrDefaultAsync(program => program.Id == programId, cancellationToken);
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

    public Task<int> CountProgramsAsync(
        Guid? organizationId,
        string? search,
        RobotProgramStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds,
        IEnumerable<Guid> allowedStoreIds,
        IEnumerable<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default)
    {
        return BuildProgramQuery(organizationId, search, status, isSystemAdmin, allowedOrganizationIds, allowedStoreIds, allowedKioskIds)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RobotProgramSummaryReadModel>> ListProgramsAsync(
        Guid? organizationId,
        string? search,
        RobotProgramStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds,
        IEnumerable<Guid> allowedStoreIds,
        IEnumerable<Guid> allowedKioskIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await BuildProgramQuery(organizationId, search, status, isSystemAdmin, allowedOrganizationIds, allowedStoreIds, allowedKioskIds)
            .OrderBy(program => program.Code)
            .ThenByDescending(program => program.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(program => new RobotProgramSummaryReadModel
            {
                Id = program.Id,
                OrganizationId = program.OrganizationId,
                StoreId = program.StoreId,
                KioskId = program.KioskId,
                DeviceId = program.DeviceId,
                Code = program.Code,
                Name = program.Name,
                ScopeType = program.ScopeType.ToString(),
                Status = program.Status.ToString(),
                Description = program.Description,
                ProgramManifestChecksum = program.ProgramManifestChecksum,
                PublishedAt = program.PublishedAt,
                ArtifactCount = program.RobotProgramArtifacts.Count
            })
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

    public Task<bool> ProgramCodeExistsAsync(
        Guid organizationId,
        Guid? storeId,
        Guid? kioskId,
        Guid? deviceId,
        string code,
        Guid? excludeProgramId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.RobotPrograms.AnyAsync(
            program => program.OrganizationId == organizationId &&
                program.StoreId == storeId &&
                program.KioskId == kioskId &&
                program.DeviceId == deviceId &&
                program.Code == code &&
                (!excludeProgramId.HasValue || program.Id != excludeProgramId.Value) &&
                program.DeletedAt == null,
            cancellationToken);
    }

    public Task<bool> ProgramScopeExistsAsync(
        TenantScopeType scopeType,
        Guid organizationId,
        Guid? storeId,
        Guid? kioskId,
        Guid? deviceId,
        CancellationToken cancellationToken = default)
    {
        return scopeType switch
        {
            TenantScopeType.Organization => _dbContext.Organizations.AnyAsync(
                organization => organization.Id == organizationId && organization.DeletedAt == null,
                cancellationToken),
            TenantScopeType.Store => _dbContext.Stores.AnyAsync(
                store => store.Id == storeId && store.OrganizationId == organizationId && store.DeletedAt == null,
                cancellationToken),
            TenantScopeType.Kiosk => _dbContext.Kiosks.AnyAsync(
                kiosk => kiosk.Id == kioskId && kiosk.StoreId == storeId && kiosk.OrganizationId == organizationId && kiosk.DeletedAt == null,
                cancellationToken),
            TenantScopeType.Device => _dbContext.Devices.AnyAsync(
                device => device.Id == deviceId && device.KioskId == kioskId &&
                    device.Kiosk != null && device.Kiosk.StoreId == storeId && device.Kiosk.OrganizationId == organizationId &&
                    device.DeletedAt == null && device.Kiosk.DeletedAt == null,
                cancellationToken),
            _ => Task.FromResult(false)
        };
    }

    public async Task<IReadOnlyList<RobotArtifact>> ListArtifactsByIdsAsync(
        IReadOnlyCollection<Guid> artifactIds,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.RobotArtifacts
            .Where(artifact => artifactIds.Contains(artifact.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RobotArtifactManifestSnapshot>> ListArtifactManifestSnapshotsAsync(
        IReadOnlyCollection<Guid> artifactIds,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.RobotArtifacts.AsNoTracking()
            .Where(artifact => artifactIds.Contains(artifact.Id))
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

    public Task<bool> ArtifactIsReferencedByDraftProgramAsync(
        Guid artifactId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.RobotProgramArtifacts.AnyAsync(
            item => item.RobotArtifactId == artifactId &&
                item.RobotProgram.Status == RobotProgramStatus.Draft,
            cancellationToken);
    }

    public Task<bool> ProgramIsReferencedByDraftReleaseAsync(
        Guid programId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ExecutionRouteRobotBindings.AnyAsync(
            binding => binding.RobotProgramId == programId &&
                binding.ExecutionRoute.ConfigurationRelease.Status == Domain.ProductionConfiguration.Enums.ConfigurationReleaseStatus.Draft,
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

    public async Task<RobotProgramDiscardOutcome> DiscardDraftProgramAsync(
        RobotProgram program,
        CancellationToken cancellationToken = default)
    {
        if (await _dbContext.ExecutionRouteRobotBindings.AnyAsync(
                binding => binding.RobotProgramId == program.Id,
                cancellationToken))
        {
            return RobotProgramDiscardOutcome.Referenced;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        _dbContext.RobotProgramArtifacts.RemoveRange(program.RobotProgramArtifacts);
        var entry = _dbContext.RobotPrograms.Remove(program);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return RobotProgramDiscardOutcome.Deleted;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            await transaction.RollbackAsync(cancellationToken);
            entry.State = EntityState.Unchanged;
            return RobotProgramDiscardOutcome.Referenced;
        }
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

    public Task AddProgramAsync(RobotProgram program, CancellationToken cancellationToken = default)
    {
        return _dbContext.RobotPrograms.AddAsync(program, cancellationToken).AsTask();
    }

    public async Task SaveProgramReplacementAsync(
        IReadOnlyCollection<RobotProgramArtifact> removedArtifacts,
        CancellationToken cancellationToken = default)
    {
        _dbContext.RobotProgramArtifacts.RemoveRange(removedArtifacts);
        await _dbContext.SaveChangesAsync(cancellationToken);
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

    private IQueryable<RobotProgram> BuildProgramQuery(
        Guid? organizationId,
        string? search,
        RobotProgramStatus? status,
        bool isSystemAdmin,
        IEnumerable<Guid> allowedOrganizationIds,
        IEnumerable<Guid> allowedStoreIds,
        IEnumerable<Guid> allowedKioskIds)
    {
        var query = _dbContext.RobotPrograms.AsNoTracking();

        if (!isSystemAdmin)
        {
            var organizationIds = allowedOrganizationIds.ToArray();
            var storeIds = allowedStoreIds.ToArray();
            var kioskIds = allowedKioskIds.ToArray();
            query = query.Where(program =>
                (program.OrganizationId.HasValue && organizationIds.Contains(program.OrganizationId.Value)) ||
                (program.StoreId.HasValue && storeIds.Contains(program.StoreId.Value)) ||
                (program.KioskId.HasValue && kioskIds.Contains(program.KioskId.Value)));
        }

        if (organizationId.HasValue)
        {
            query = query.Where(program => program.OrganizationId == organizationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(program =>
                EF.Functions.ILike(program.Code, $"%{term}%") ||
                EF.Functions.ILike(program.Name, $"%{term}%"));
        }

        if (status.HasValue)
        {
            query = query.Where(program => program.Status == status.Value);
        }

        return query;
    }
}
