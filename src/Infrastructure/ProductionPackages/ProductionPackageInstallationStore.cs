using Application.ProductionPackages.Installation;
using Domain.Catalog.Entities;
using Domain.Common;
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

    public async Task<bool> TryRestartFailedAsync(
        Guid organizationId, Guid installationId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var affected = await db.ProductionPackageInstallations
            .Where(x => x.OrganizationId == organizationId && x.Id == installationId &&
                x.Status == ProductionPackageInstallationStatus.Failed)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, ProductionPackageInstallationStatus.Pending)
                .SetProperty(x => x.FailureCode, (string?)null)
                .SetProperty(x => x.FailureMessage, (string?)null)
                .SetProperty(x => x.CompletedAt, (DateTimeOffset?)null)
                .SetProperty(x => x.StartedAt, now), cancellationToken);
        db.ChangeTracker.Clear();
        return affected == 1;
    }

    public Task<ProductionPackageInstallationStatus?> GetCurrentStatusAsync(
        Guid organizationId, Guid installationId, CancellationToken cancellationToken) =>
        db.ProductionPackageInstallations.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.Id == installationId)
            .Select(x => (ProductionPackageInstallationStatus?)x.Status)
            .SingleOrDefaultAsync(cancellationToken);

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

    public async Task<IReadOnlyList<RobotArtifact>> ListArtifactsByCodesAsync(
        Guid organizationId,
        IReadOnlyCollection<string> artifactCodes,
        CancellationToken cancellationToken) =>
        await db.RobotArtifacts.AsNoTracking()
            .Where(artifact => artifact.OrganizationId == organizationId &&
                artifactCodes.Contains(artifact.ArtifactCode))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlySet<Guid>> ListPackageManagedArtifactIdsAsync(
        IReadOnlyCollection<Guid> artifactIds,
        CancellationToken cancellationToken)
    {
        if (artifactIds.Count == 0) return new HashSet<Guid>();
        var targetKeys = artifactIds.Select(id => id.ToString("D")).ToArray();
        var managedKeys = await db.ProductionPackageMaterializations.AsNoTracking()
            .Where(materialization =>
                materialization.ResourceKind == ProductionPackageResourceKind.RobotArtifact &&
                targetKeys.Contains(materialization.TargetKey) &&
                materialization.Installation.OwnershipMode == ProductionPackageOwnershipMode.PackageManaged &&
                (materialization.Installation.Status == ProductionPackageInstallationStatus.Installed ||
                 materialization.Installation.Status == ProductionPackageInstallationStatus.Superseded))
            .Select(materialization => materialization.TargetKey)
            .Distinct()
            .ToArrayAsync(cancellationToken);
        return managedKeys.Select(Guid.Parse).ToHashSet();
    }

    public async Task<ProductionPackageForkGraph?> GetForkGraphAsync(
        Guid organizationId,
        Guid installationId,
        bool tracked,
        CancellationToken cancellationToken)
    {
        var installation = await (tracked ? Graph(tracked: true) : Graph())
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == installationId,
                cancellationToken);
        if (installation is null) return null;

        var artifactIds = installation.Materializations
            .Where(x => x.ResourceKind == ProductionPackageResourceKind.RobotArtifact)
            .Select(x => Guid.TryParse(x.TargetKey, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        var programIds = installation.Materializations
            .Where(x => x.ResourceKind == ProductionPackageResourceKind.RobotProgram)
            .Select(x => Guid.TryParse(x.TargetKey, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        IQueryable<RobotArtifact> artifactQuery = db.RobotArtifacts;
        IQueryable<RobotProgram> programQuery = db.RobotPrograms.Include(x => x.RobotProgramArtifacts);
        if (!tracked)
        {
            artifactQuery = artifactQuery.AsNoTracking();
            programQuery = programQuery.AsNoTracking();
        }
        var artifacts = await artifactQuery
            .Where(x => x.OrganizationId == organizationId && artifactIds.Contains(x.Id))
            .ToArrayAsync(cancellationToken);
        var programs = await programQuery
            .Where(x => x.OrganizationId == organizationId && programIds.Contains(x.Id))
            .ToArrayAsync(cancellationToken);

        var artifactTargetKeys = artifactIds.Select(id => id.ToString("D")).ToArray();
        var sharedTargetKeys = await db.ProductionPackageMaterializations.AsNoTracking()
            .Where(materialization =>
                materialization.InstallationId != installationId &&
                materialization.ResourceKind == ProductionPackageResourceKind.RobotArtifact &&
                artifactTargetKeys.Contains(materialization.TargetKey) &&
                materialization.Installation.OwnershipMode == ProductionPackageOwnershipMode.PackageManaged &&
                (materialization.Installation.Status == ProductionPackageInstallationStatus.Installed ||
                 materialization.Installation.Status == ProductionPackageInstallationStatus.Superseded))
            .Select(materialization => materialization.TargetKey)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        return new ProductionPackageForkGraph(
            installation,
            artifacts,
            programs,
            sharedTargetKeys.Select(Guid.Parse).ToHashSet());
    }

    public Task<bool> HasActiveUpgradeAsync(
        Guid organizationId,
        Guid installationId,
        CancellationToken cancellationToken) =>
        db.ProductionPackageUpgrades.AsNoTracking().AnyAsync(item =>
            item.OrganizationId == organizationId &&
            (item.SourceInstallationId == installationId || item.TargetInstallationId == installationId) &&
            (item.Status == ProductionPackageUpgradeStatus.Materializing ||
             item.Status == ProductionPackageUpgradeStatus.ReadyForReview ||
             item.Status == ProductionPackageUpgradeStatus.RollbackPending),
            cancellationToken);

    public async Task PersistForkAsync(
        ProductionPackageInstallation installation,
        IReadOnlyCollection<RobotArtifact> artifacts,
        IReadOnlyCollection<RobotProgramArtifact> removedProgramArtifacts,
        CancellationToken cancellationToken)
    {
        db.RobotProgramArtifacts.RemoveRange(removedProgramArtifacts);
        db.RobotArtifacts.AddRange(artifacts);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductionPackageMaterializationRepairResult> RestoreSoftDeletedMaterializationsAsync(
        Guid organizationId, Guid installationId, Guid actorId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({installationId.ToString("D")}, 0));",
            cancellationToken);

        var installation = await Graph(tracked: true)
            .Where(x => x.Id == installationId && x.OrganizationId == organizationId)
            .SingleOrDefaultAsync(cancellationToken);
        if (installation is null)
            return RepairIssue("Installation", string.Empty, installationId.ToString("D"), "InstallationNotFound");
        IReadOnlyCollection<ProductionPackageMaterializationExpectation> expectedMaterializations;
        try
        {
            expectedMaterializations = ProductionPackageMaterializationExpectationBuilder.Build(installation);
        }
        catch (DomainRuleException)
        {
            return RepairIssue("Materialization", string.Empty, string.Empty, "InstallationSnapshotInvalid");
        }

        var rows = await db.ProductionPackageMaterializations.AsNoTracking()
            .Where(x => x.InstallationId == installationId && x.Installation.OrganizationId == organizationId)
            .Select(x => new MaterializationIdentity(x.ResourceKind, x.SourceKey, x.TargetKey))
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return RepairIssue("Materialization", string.Empty, string.Empty, "MaterializationEvidenceMissing");

        var missingEvidence = expectedMaterializations.Where(expected => rows.Count(row =>
                row.ResourceKind == expected.ResourceKind &&
                (expected.SourceKey is null || row.SourceKey == expected.SourceKey) &&
                (!expected.ExpectedTargetId.HasValue ||
                 string.Equals(row.TargetKey, expected.ExpectedTargetId.Value.ToString("D"),
                     StringComparison.OrdinalIgnoreCase))) < expected.ExpectedCount)
            .Select(expected => new ProductionPackageMaterializationRepairIssue(
                expected.ResourceKind.ToString(), expected.SourceKey ?? string.Empty, string.Empty,
                "MaterializationEvidenceMissing"))
            .ToArray();
        if (missingEvidence.Length > 0)
            return new ProductionPackageMaterializationRepairResult([], missingEvidence);

        var candidates = new Dictionary<(ProductionPackageResourceKind Kind, Guid Id), RepairCandidate>();
        var issues = new List<ProductionPackageMaterializationRepairIssue>();
        foreach (var row in rows)
        {
            if (!Guid.TryParse(row.TargetKey, out var targetId))
            {
                issues.Add(Issue(row, "InvalidTargetIdentity"));
                continue;
            }

            BusinessEntity? target;
            bool existsOutsideScope;
            switch (row.ResourceKind)
            {
                case ProductionPackageResourceKind.Product:
                    (target, existsOutsideScope) = await FindTargetAsync(db.Products, targetId,
                        query => query.Where(x => x.OrganizationId == organizationId &&
                            x.StoreId == installation.StoreId && x.KioskId == installation.KioskId), cancellationToken);
                    break;
                case ProductionPackageResourceKind.ProductVariant:
                    (target, existsOutsideScope) = await FindTargetAsync(db.ProductVariants, targetId,
                        query => query.Where(x => x.Product.OrganizationId == organizationId &&
                            x.Product.StoreId == installation.StoreId && x.Product.KioskId == installation.KioskId), cancellationToken);
                    break;
                case ProductionPackageResourceKind.ProductOption:
                    (target, existsOutsideScope) = await FindTargetAsync(db.ProductOptions, targetId,
                        query => query.Where(x => x.OptionGroup.Product.OrganizationId == organizationId &&
                            x.OptionGroup.Product.StoreId == installation.StoreId &&
                            x.OptionGroup.Product.KioskId == installation.KioskId), cancellationToken);
                    break;
                case ProductionPackageResourceKind.Recipe:
                    (target, existsOutsideScope) = await FindTargetAsync(db.Recipes, targetId,
                        query => query.Where(x => x.OrganizationId == organizationId &&
                            x.StoreId == installation.StoreId && x.KioskId == installation.KioskId), cancellationToken);
                    break;
                case ProductionPackageResourceKind.RobotArtifact:
                    (target, existsOutsideScope) = await FindTargetAsync(db.RobotArtifacts, targetId,
                        query => query.Where(x => x.OrganizationId == organizationId), cancellationToken);
                    break;
                case ProductionPackageResourceKind.RobotProgram:
                    (target, existsOutsideScope) = await FindTargetAsync(db.RobotPrograms, targetId,
                        query => query.Where(x => x.OrganizationId == organizationId &&
                            x.StoreId == installation.StoreId && x.KioskId == installation.KioskId), cancellationToken);
                    break;
                case ProductionPackageResourceKind.ConfigurationRelease:
                    (target, existsOutsideScope) = await FindTargetAsync(db.ConfigurationReleases, targetId,
                        query => query.Where(x => x.OrganizationId == organizationId), cancellationToken);
                    break;
                default:
                    issues.Add(Issue(row, "UnsupportedResourceKind"));
                    continue;
            }

            if (target is null)
            {
                issues.Add(Issue(row, existsOutsideScope ? "TargetScopeMismatch" : "TargetPhysicallyMissing"));
                continue;
            }
            if (target.DeletedAt.HasValue)
                candidates.TryAdd((row.ResourceKind, targetId), new RepairCandidate(row, target));
        }

        if (issues.Count > 0)
            return new ProductionPackageMaterializationRepairResult([], issues);

        var now = DateTimeOffset.UtcNow;
        foreach (var candidate in candidates.Values)
        {
            candidate.Target.DeletedAt = null;
            candidate.Target.DeletedByAccountId = null;
            candidate.Target.UpdatedAt = now;
            candidate.Target.UpdatedByAccountId = actorId;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return RepairIssue("Materialization", string.Empty, string.Empty, "RestoreConstraintConflict");
        }

        return new ProductionPackageMaterializationRepairResult(
            candidates.Values.Select(x => new ProductionPackageMaterializationRepairItem(
                x.Row.ResourceKind.ToString(), x.Row.SourceKey, x.Row.TargetKey)).ToArray(), []);
    }

    private static async Task<(BusinessEntity? Target, bool ExistsOutsideScope)> FindTargetAsync<TEntity>(
        DbSet<TEntity> set, Guid id, Func<IQueryable<TEntity>, IQueryable<TEntity>> applyScope,
        CancellationToken cancellationToken) where TEntity : BusinessEntity
    {
        var byId = set.IgnoreQueryFilters().Where(x => x.Id == id);
        var target = await applyScope(byId).SingleOrDefaultAsync(cancellationToken);
        return target is not null
            ? (target, false)
            : (null, await byId.AnyAsync(cancellationToken));
    }

    private static ProductionPackageMaterializationRepairResult RepairIssue(
        string kind, string sourceKey, string targetKey, string code) =>
        new([], [new ProductionPackageMaterializationRepairIssue(kind, sourceKey, targetKey, code)]);

    private static ProductionPackageMaterializationRepairIssue Issue(MaterializationIdentity row, string code) =>
        new(row.ResourceKind.ToString(), row.SourceKey, row.TargetKey, code);

    private sealed record MaterializationIdentity(
        ProductionPackageResourceKind ResourceKind, string SourceKey, string TargetKey);
    private sealed record RepairCandidate(MaterializationIdentity Row, BusinessEntity Target);

    public async Task MarkFailedAsync(Guid organizationId, Guid installationId, string failureCode,
        string failureMessage, CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        var installation = await GetForEditAsync(organizationId, installationId, cancellationToken)
            ?? throw new InvalidOperationException("Package installation disappeared while recording failure.");
        if (installation.Status is ProductionPackageInstallationStatus.Installed or
            ProductionPackageInstallationStatus.Superseded or
            ProductionPackageInstallationStatus.Failed)
            return;
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
        var ownsTransaction = db.Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
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
        if (ownsTransaction)
            await transaction!.CommitAsync(cancellationToken);
        return release;
    }

    private IQueryable<ProductionPackageInstallation> Graph(bool tracked = false)
    {
        var query = tracked ? db.ProductionPackageInstallations : db.ProductionPackageInstallations.AsNoTracking();
        return query.Include(x => x.PackageVersion).ThenInclude(x => x.ProductionPackage)
            .Include(x => x.PackageVersion).ThenInclude(x => x.Products)
            .Include(x => x.PackageVersion).ThenInclude(x => x.Artifacts)
            .Include(x => x.PackageVersion).ThenInclude(x => x.Routes)
            .Include(x => x.Materializations)
            .AsSplitQuery();
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
