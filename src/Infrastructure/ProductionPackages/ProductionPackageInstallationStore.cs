using Application.ProductionPackages.Installation;
using Domain.Catalog.Entities;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionPackages;
using Domain.RobotConfiguration.Artifacts;
using Domain.RobotConfiguration.Programs;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;

namespace Infrastructure.ProductionPackages;

public sealed class ProductionPackageInstallationStore(IceBotDbContext db) : IProductionPackageInstallationStore
{
    public async Task<bool> ScopeExistsAsync(Guid organizationId, Guid? storeId, Guid? kioskId, CancellationToken cancellationToken)
    {
        if (!await db.Organizations.AnyAsync(x => x.Id == organizationId && x.DeletedAt == null, cancellationToken)) return false;
        if (storeId.HasValue && !await db.Stores.AnyAsync(x => x.Id == storeId && x.OrganizationId == organizationId && x.DeletedAt == null, cancellationToken)) return false;
        if (kioskId.HasValue && !await db.Kiosks.AnyAsync(x => x.Id == kioskId && x.OrganizationId == organizationId &&
            (!storeId.HasValue || x.StoreId == storeId) && x.DeletedAt == null, cancellationToken)) return false;
        return !kioskId.HasValue || storeId.HasValue;
    }

    public Task<ProductionPackageInstallation?> FindByIdempotencyKeyAsync(Guid organizationId, string key, CancellationToken cancellationToken) =>
        Graph(tracked: true).FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.IdempotencyKey == key, cancellationToken);

    public Task<ProductionPackageInstallation?> GetAsync(Guid organizationId, Guid installationId, CancellationToken cancellationToken) =>
        Graph().FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == installationId, cancellationToken);

    public Task<ProductionPackageInstallation?> GetForEditAsync(Guid organizationId, Guid installationId, CancellationToken cancellationToken) =>
        Graph(tracked: true).FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == installationId, cancellationToken);

    public Task<int> CountAsync(Guid organizationId, ProductionPackageInstallationStatus? status, Guid? storeId,
        Guid? kioskId, CancellationToken cancellationToken) =>
        Filter(organizationId, status, storeId, kioskId).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductionPackageInstallation>> ListAsync(Guid organizationId,
        ProductionPackageInstallationStatus? status, Guid? storeId, Guid? kioskId, int pageNumber, int pageSize,
        CancellationToken cancellationToken) => await Filter(organizationId, status, storeId, kioskId)
        .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
        .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

    public async Task<ProductionPackageInstallationInsertResult> InsertOrGetAsync(
        ProductionPackageInstallation installation, CancellationToken cancellationToken)
    {
        EntityEntry<ProductionPackageInstallation> entry = db.ProductionPackageInstallations.Add(installation);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return new ProductionPackageInstallationInsertResult(true, installation);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            entry.State = EntityState.Detached;
            var existing = await FindByIdempotencyKeyAsync(installation.OrganizationId, installation.IdempotencyKey, cancellationToken)
                ?? throw new InvalidOperationException("Concurrent package installation could not be reloaded.", ex);
            return new ProductionPackageInstallationInsertResult(false, existing);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public async Task MarkFailedAsync(Guid organizationId, Guid installationId, string failureCode,
        string failureMessage, CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        var installation = await GetForEditAsync(organizationId, installationId, cancellationToken)
            ?? throw new InvalidOperationException("Package installation disappeared while recording failure.");
        installation.Fail(failureCode, failureMessage, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ConfigurationRelease> PersistMaterializedGraphAsync(
        ProductionPackageInstallation installation,
        IReadOnlyCollection<Product> products,
        IReadOnlyCollection<RobotArtifact> artifacts,
        IReadOnlyCollection<RobotProgram> programs,
        IReadOnlyCollection<ProductionComposition> compositions,
        Func<long, ConfigurationRelease> releaseFactory,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({installation.OrganizationId.ToString("D")}, 0));", cancellationToken);
        var maximum = await db.ConfigurationReleases.WhereNotDeleted()
            .Where(x => x.OrganizationId == installation.OrganizationId)
            .Select(x => (long?)x.ReleaseNumber).MaxAsync(cancellationToken);
        var release = releaseFactory((maximum ?? 0) + 1);
        installation.AddMaterialization(ProductionPackageResourceKind.ConfigurationRelease,
            $"PACKAGE_RELEASE_{release.ReleaseNumber}", release.Id.ToString("D"));
        installation.Complete(release.Id, DateTimeOffset.UtcNow);
        db.Products.AddRange(products);
        db.RobotArtifacts.AddRange(artifacts);
        db.RobotPrograms.AddRange(programs);
        db.ProductionCompositions.AddRange(compositions);
        db.ConfigurationReleases.Add(release);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return release;
    }

    private IQueryable<ProductionPackageInstallation> Graph(bool tracked = false)
    {
        var query = tracked ? db.ProductionPackageInstallations : db.ProductionPackageInstallations.AsNoTracking();
        return query.Include(x => x.PackageVersion).ThenInclude(x => x.ProductionPackage)
            .Include(x => x.Materializations);
    }

    private IQueryable<ProductionPackageInstallation> Filter(Guid organizationId,
        ProductionPackageInstallationStatus? status, Guid? storeId, Guid? kioskId)
    {
        var query = Graph().Where(x => x.OrganizationId == organizationId);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (storeId.HasValue) query = query.Where(x => x.StoreId == storeId.Value);
        if (kioskId.HasValue) query = query.Where(x => x.KioskId == kioskId.Value);
        return query;
    }
}
