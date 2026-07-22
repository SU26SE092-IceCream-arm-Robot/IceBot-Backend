using Application.ProductionPackages;
using Domain.Catalog.Entities;
using Domain.ProductionPackages;
using Domain.RobotConfiguration.ArtifactContracts;
using Domain.RobotConfiguration.ArtifactTemplates;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Infrastructure.ProductionPackages;

public sealed class ProductionPackageStore(IceBotDbContext db) : IProductionPackageStore
{
    public Task<ProductionPackage?> GetPackageAsync(Guid id, bool tracked, CancellationToken cancellationToken)
    {
        var query = tracked ? db.ProductionPackages : db.ProductionPackages.AsNoTracking();
        return query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<ProductionPackage?> GetPackageWithVersionsAsync(Guid id, bool tracked, CancellationToken cancellationToken)
    {
        var query = tracked ? db.ProductionPackages : db.ProductionPackages.AsNoTracking();
        return query.Include(x => x.Versions).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductionPackage>> ListPackagesAsync(CancellationToken cancellationToken) =>
        await db.ProductionPackages.AsNoTracking().Include(x => x.Versions)
            .OrderBy(x => x.Code).ToListAsync(cancellationToken);

    public Task<ProductionPackageVersion?> GetVersionAsync(Guid packageId, Guid versionId, bool tracked, CancellationToken cancellationToken)
    {
        var query = tracked ? db.ProductionPackageVersions : db.ProductionPackageVersions.AsNoTracking();
        return query.Include(x => x.ProductionPackage)
            .Include(x => x.Products).Include(x => x.Artifacts)
            .Include(x => x.Programs).ThenInclude(x => x.Slots).Include(x => x.Routes)
            .FirstOrDefaultAsync(x => x.ProductionPackageId == packageId && x.Id == versionId, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductionPackageVersion>> ListPublishedAsync(CancellationToken cancellationToken) =>
        await db.ProductionPackageVersions.AsNoTracking().Include(x => x.ProductionPackage).Include(x => x.Products)
            .Where(x => x.Status == ProductionPackageVersionStatus.Published && x.ProductionPackage.Status == ProductionPackageStatus.Active)
            .OrderBy(x => x.ProductionPackage.Code).ThenByDescending(x => x.Version).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Product>> LoadGlobalProductsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await db.Products.AsNoTracking().Where(x => ids.Contains(x.Id))
            .Include(x => x.ProductVariants).ThenInclude(x => x.Recipes).ThenInclude(x => x.RecipeItems).ThenInclude(x => x.Ingredient)
            .Include(x => x.OptionGroups).ThenInclude(x => x.ProductOptions).ThenInclude(x => x.IngredientRequirements).ThenInclude(x => x.Ingredient)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RobotArtifactTemplate>> LoadArtifactTemplatesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await db.RobotArtifactTemplates.AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RobotArtifactTechnicalContract>> LoadTechnicalContractsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await db.RobotArtifactTechnicalContracts.AsNoTracking().Where(x => ids.Contains(x.Id))
            .Include(x => x.Effects).Include(x => x.OrderingConstraints).ToListAsync(cancellationToken);

    public async Task AddPackageAsync(ProductionPackage package, CancellationToken cancellationToken)
    {
        db.ProductionPackages.Add(package);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductionPackageVersion> CreateNextVersionAsync(Guid packageId, Guid? actorId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({packageId.ToString("D")}, 0));", cancellationToken);
        var nextVersion = (await db.ProductionPackageVersions.Where(x => x.ProductionPackageId == packageId)
            .Select(x => (int?)x.Version).MaxAsync(cancellationToken) ?? 0) + 1;
        var version = ProductionPackageVersion.CreateDraft(packageId, nextVersion);
        version.CreatedByAccountId = actorId;
        db.ProductionPackageVersions.Add(version);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return version;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
