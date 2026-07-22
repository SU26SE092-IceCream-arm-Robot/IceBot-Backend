using Application.RobotConfiguration.AuthoringImports;
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.AuthoringImports;
using Domain.RobotConfiguration.Programs;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Application.Shared.Concurrency;

namespace Infrastructure.RobotConfiguration.AuthoringImports.Persistence;

public sealed class RobotAuthoringImportStore(IceBotDbContext dbContext) : IRobotAuthoringImportStore
{
    private IDbContextTransaction? _mutationTransaction;
    public Task<bool> ScopeExistsAsync(Guid organizationId, Guid? storeId, Guid? kioskId, Guid? deviceId,
        CancellationToken cancellationToken)
    {
        if (deviceId.HasValue)
            return dbContext.Devices.AnyAsync(x => x.Id == deviceId && x.KioskId == kioskId &&
                x.Kiosk != null && x.Kiosk.OrganizationId == organizationId && x.Kiosk.StoreId == storeId,
                cancellationToken);
        if (kioskId.HasValue)
            return dbContext.Kiosks.AnyAsync(x => x.Id == kioskId && x.OrganizationId == organizationId &&
                x.StoreId == storeId, cancellationToken);
        if (storeId.HasValue)
            return dbContext.Stores.AnyAsync(x => x.Id == storeId && x.OrganizationId == organizationId,
                cancellationToken);
        return dbContext.Organizations.AnyAsync(x => x.Id == organizationId, cancellationToken);
    }

    public Task<RobotAuthoringImport?> GetByIdempotencyKeyAsync(Guid organizationId, string idempotencyKey,
        bool tracked, CancellationToken cancellationToken) =>
        Query(tracked).FirstOrDefaultAsync(x => x.OrganizationId == organizationId &&
            x.IdempotencyKey == idempotencyKey, cancellationToken);

    public Task<RobotAuthoringImport?> GetAsync(Guid organizationId, Guid importId, bool tracked,
        CancellationToken cancellationToken) =>
        Query(tracked).FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == importId,
            cancellationToken);

    public async Task<IReadOnlyList<RobotArtifactTechnicalContract>> GetContractsAsync(Guid organizationId,
        IReadOnlyCollection<string> codes, bool tracked, CancellationToken cancellationToken)
    {
        IQueryable<RobotArtifactTechnicalContract> query = dbContext.RobotArtifactTechnicalContracts
            .Include(x => x.Effects).Include(x => x.OrderingConstraints)
            .Where(x => x.OrganizationId == organizationId && x.ContractVersion == 1 && codes.Contains(x.ContractCode));
        if (!tracked) query = query.AsNoTracking();
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RobotArtifact>> GetArtifactsAsync(Guid organizationId,
        IReadOnlyCollection<string> codes, bool tracked, CancellationToken cancellationToken)
    {
        IQueryable<RobotArtifact> query = dbContext.RobotArtifacts
            .Where(x => x.OrganizationId == organizationId && codes.Contains(x.ArtifactCode));
        if (!tracked) query = query.AsNoTracking();
        return await query.ToListAsync(cancellationToken);
    }

    public Task<RobotProgram?> GetProgramAsync(Guid organizationId, Guid? storeId, Guid? kioskId, Guid? deviceId,
        string code, bool tracked, CancellationToken cancellationToken)
    {
        IQueryable<RobotProgram> query = dbContext.RobotPrograms.Include(x => x.RobotProgramArtifacts)
            .Where(x => x.OrganizationId == organizationId && x.StoreId == storeId && x.KioskId == kioskId &&
                x.DeviceId == deviceId && x.Code == code);
        if (!tracked) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Created, RobotAuthoringImport Import)> InsertOrGetExistingAsync(
        RobotAuthoringImport importSession, CancellationToken cancellationToken)
    {
        var entry = dbContext.RobotAuthoringImports.Add(importSession);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return (true, importSession);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            foreach (var item in importSession.Items.ToArray())
                dbContext.Entry(item).State = EntityState.Detached;
            entry.State = EntityState.Detached;
            var existing = await Query(false).FirstOrDefaultAsync(x => x.OrganizationId == importSession.OrganizationId &&
                x.IdempotencyKey == importSession.IdempotencyKey, cancellationToken);
            if (existing is null) throw;
            return (false, existing);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    public async Task<RobotAuthoringImport?> BeginMutationAsync(Guid organizationId, Guid importId,
        CancellationToken cancellationToken)
    {
        if (_mutationTransaction is not null)
            throw new InvalidOperationException("A robot authoring import mutation transaction is already active.");
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var lockIdentity = $"robot-authoring-import:{importId:D}";
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({lockIdentity}, 0))",
                cancellationToken);
            var importSession = await Query(true).FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == importId, cancellationToken);
            _mutationTransaction = transaction;
            return importSession;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            await transaction.DisposeAsync();
            throw;
        }
    }

    public async Task LockApplyResourceIdentitiesAsync(
        Guid organizationId,
        Guid? storeId,
        Guid? kioskId,
        Guid? deviceId,
        string programCode,
        IReadOnlyCollection<string> artifactCodes,
        CancellationToken cancellationToken)
    {
        if (_mutationTransaction is null)
            throw new InvalidOperationException("Resource identities can only be locked inside an import apply transaction.");

        var lockIdentities = TechnicalResourceMutationIdentity.OrderForLocking(artifactCodes
            .SelectMany(code => new[]
            {
                TechnicalResourceMutationIdentity.ArtifactDefinition(organizationId, code),
                TechnicalResourceMutationIdentity.ContractDefinition(organizationId, code, 1)
            })
            .Append(TechnicalResourceMutationIdentity.ProgramDefinition(
                organizationId, storeId, kioskId, deviceId, programCode)));

        foreach (var lockIdentity in lockIdentities)
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({lockIdentity.AdvisoryLockKey}, 0))",
                cancellationToken);
    }

    public async Task CommitMutationAsync(CancellationToken cancellationToken)
    {
        if (_mutationTransaction is null)
            throw new InvalidOperationException("No robot authoring import mutation transaction is active.");
        await dbContext.SaveChangesAsync(cancellationToken);
        await _mutationTransaction.CommitAsync(cancellationToken);
        await _mutationTransaction.DisposeAsync();
        _mutationTransaction = null;
    }

    public async Task RollbackMutationAsync(CancellationToken cancellationToken)
    {
        if (_mutationTransaction is null) return;
        var transaction = _mutationTransaction;
        _mutationTransaction = null;
        try
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await transaction.DisposeAsync();
            dbContext.ChangeTracker.Clear();
        }
    }

    public async Task PrepareApplyAsync(IReadOnlyCollection<RobotArtifactTechnicalContract> contracts,
        IReadOnlyCollection<RobotArtifact> artifacts, RobotProgram? program, CancellationToken cancellationToken)
    {
        if (_mutationTransaction is null)
            throw new InvalidOperationException("Apply must run inside a robot authoring import mutation transaction.");
        if (contracts.Count > 0) dbContext.RobotArtifactTechnicalContracts.AddRange(contracts);
        if (artifacts.Count > 0) dbContext.RobotArtifacts.AddRange(artifacts);
        if (program is not null && dbContext.Entry(program).State == EntityState.Detached)
            dbContext.RobotPrograms.Add(program);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CommitPreparedMutationAsync(CancellationToken cancellationToken)
    {
        if (_mutationTransaction is null)
            throw new InvalidOperationException("No prepared robot authoring import mutation transaction is active.");
        var transaction = _mutationTransaction;
        await transaction.CommitAsync(cancellationToken);
        _mutationTransaction = null;
        await transaction.DisposeAsync();
    }

    private IQueryable<RobotAuthoringImport> Query(bool tracked)
    {
        IQueryable<RobotAuthoringImport> query = dbContext.RobotAuthoringImports.Include(x => x.Items);
        return tracked ? query : query.AsNoTracking();
    }
}
