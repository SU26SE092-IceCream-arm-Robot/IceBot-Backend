using Application.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.ArtifactContracts;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.RobotConfiguration.ArtifactContracts;

public sealed class RobotArtifactTechnicalContractStore(IceBotDbContext db) : IRobotArtifactTechnicalContractStore
{
    public Task<RobotArtifactTechnicalContract?> GetAsync(Guid id, bool tracked, CancellationToken cancellationToken)
    {
        var query = tracked ? db.RobotArtifactTechnicalContracts : db.RobotArtifactTechnicalContracts.AsNoTracking();
        return query.Include(x => x.Effects).Include(x => x.OrderingConstraints)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<RobotArtifactTechnicalContract?> GetByIdentityAsync(
        Guid? organizationId,
        string code,
        int version,
        bool tracked,
        CancellationToken cancellationToken)
    {
        var query = tracked ? db.RobotArtifactTechnicalContracts : db.RobotArtifactTechnicalContracts.AsNoTracking();
        return query.Include(x => x.Effects).Include(x => x.OrderingConstraints)
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.ContractCode == code &&
                x.ContractVersion == version, cancellationToken);
    }

    public Task<int> CountAsync(
        Guid? organizationId,
        RobotArtifactContractStatus? status,
        string? search,
        bool publishedOnly,
        CancellationToken cancellationToken) =>
        Filter(db.RobotArtifactTechnicalContracts.AsNoTracking(), organizationId, status, search, publishedOnly)
            .CountAsync(cancellationToken);

    public async Task<IReadOnlyList<RobotArtifactTechnicalContract>> ListAsync(
        Guid? organizationId,
        RobotArtifactContractStatus? status,
        string? search,
        bool publishedOnly,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken) =>
        await Filter(db.RobotArtifactTechnicalContracts.AsNoTracking(), organizationId, status, search, publishedOnly)
            .OrderBy(x => x.ContractCode)
            .ThenByDescending(x => x.ContractVersion)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(x => x.Effects)
            .Include(x => x.OrderingConstraints)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

    public Task<bool> VersionExistsAsync(Guid? organizationId, string code, int version, CancellationToken cancellationToken) =>
        db.RobotArtifactTechnicalContracts.AnyAsync(x => x.OrganizationId == organizationId && x.ContractCode == code &&
            x.ContractVersion == version, cancellationToken);

    public Task<bool> HasPublishedTemplateReferenceAsync(Guid contractId, CancellationToken cancellationToken) =>
        db.RobotArtifactTemplates.AnyAsync(
            template => template.TechnicalContractId == contractId &&
                template.Status == Domain.RobotConfiguration.Artifacts.RobotArtifactStatus.Published,
            cancellationToken);

    public async Task AddAsync(RobotArtifactTechnicalContract contract, CancellationToken cancellationToken)
    {
        db.RobotArtifactTechnicalContracts.Add(contract);
        await db.SaveChangesAsync(cancellationToken);
    }

    public void Remove(RobotArtifactTechnicalContract contract) => db.RobotArtifactTechnicalContracts.Remove(contract);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    private static IQueryable<RobotArtifactTechnicalContract> Filter(
        IQueryable<RobotArtifactTechnicalContract> query,
        Guid? organizationId,
        RobotArtifactContractStatus? status,
        string? search,
        bool publishedOnly)
    {
        query = query.Where(x => x.OrganizationId == organizationId);
        if (publishedOnly)
            query = query.Where(x => x.Status == RobotArtifactContractStatus.Published);
        else if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.ContractCode, $"%{term}%") ||
                EF.Functions.ILike(x.RuntimeTargetCode, $"%{term}%") ||
                EF.Functions.ILike(x.MachineModelCode, $"%{term}%"));
        }

        return query;
    }
}
