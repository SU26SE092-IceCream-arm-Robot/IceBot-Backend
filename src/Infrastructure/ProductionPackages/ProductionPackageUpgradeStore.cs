using Application.ProductionPackages.Upgrades;
using Application.ProductionConfiguration.Deployments.ReadModels;
using Domain.Catalog.Entities;
using Domain.Devices.ExecutionEndpoints;
using Domain.ProductionPackages;
using Domain.ProductionConfiguration.Entities;
using Domain.ProductionConfiguration.Enums;
using Domain.SalesCatalog.Entities;
using Domain.RobotConfiguration.Artifacts;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.ProductionPackages;

public sealed class ProductionPackageUpgradeStore(IceBotDbContext db) : IProductionPackageUpgradeStore
{
    public Task<ProductionPackageInstallation?> GetSourceInstallationAsync(
        Guid organizationId, Guid sourceInstallationId, CancellationToken cancellationToken) =>
        db.ProductionPackageInstallations.AsNoTracking().SingleOrDefaultAsync(
            item => item.OrganizationId == organizationId && item.Id == sourceInstallationId,
            cancellationToken);

    public async Task<ProductionPackageUpgradeSourceState?> GetSourceStateAsync(
        Guid organizationId, Guid sourceInstallationId, bool tracked, CancellationToken cancellationToken)
    {
        var installation = await InstallationQuery(tracked)
            .SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.Id == sourceInstallationId,
                cancellationToken);
        if (installation is null) return null;
        var resources = await LoadResourcesAsync(installation, tracked, cancellationToken);
        var menus = await LoadMenuItemsAsync(resources.Products.Values.Select(item => item.Id).ToArray(), tracked,
            cancellationToken);
        var endpoints = await LoadEndpointTargetsAsync(installation, tracked, cancellationToken);
        return new ProductionPackageUpgradeSourceState(installation, resources, menus, endpoints);
    }

    public async Task<ProductionPackageUpgradePreparationState?> GetPreparationStateAsync(
        Guid organizationId, Guid upgradeId, Guid targetInstallationId, CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        var upgrade = await UpgradeQuery(tracked: true)
            .SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.Id == upgradeId,
                cancellationToken);
        if (upgrade is null || upgrade.Status != ProductionPackageUpgradeStatus.Materializing) return null;
        var source = await InstallationQuery(tracked: true)
            .SingleOrDefaultAsync(item => item.Id == upgrade.SourceInstallationId &&
                                          item.OrganizationId == organizationId, cancellationToken);
        var target = await InstallationQuery(tracked: true)
            .SingleOrDefaultAsync(item => item.Id == targetInstallationId &&
                                          item.OrganizationId == organizationId, cancellationToken);
        if (source is null || target is null) return null;
        var sourceResources = await LoadResourcesAsync(source, tracked: true, cancellationToken);
        var targetResources = await LoadResourcesAsync(target, tracked: true, cancellationToken);
        var menus = await LoadMenuItemsAsync(sourceResources.Products.Values.Select(item => item.Id).ToArray(),
            tracked: true, cancellationToken);
        var endpoints = await LoadEndpointTargetsAsync(source, tracked: true, cancellationToken);
        return new ProductionPackageUpgradePreparationState(upgrade, source, target, sourceResources,
            targetResources, menus, endpoints);
    }

    public async Task<ProductionPackageUpgradeMutationState?> GetMutationStateAsync(
        Guid organizationId, Guid upgradeId, CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        var upgrade = await UpgradeQuery(tracked: true)
            .SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.Id == upgradeId,
                cancellationToken);
        if (upgrade?.TargetInstallationId is null) return null;
        var source = await InstallationQuery(tracked: true).SingleOrDefaultAsync(
            item => item.Id == upgrade.SourceInstallationId && item.OrganizationId == organizationId,
            cancellationToken);
        var target = await InstallationQuery(tracked: true).SingleOrDefaultAsync(
            item => item.Id == upgrade.TargetInstallationId && item.OrganizationId == organizationId,
            cancellationToken);
        if (source?.DraftConfigurationReleaseId is null || target?.DraftConfigurationReleaseId is null) return null;
        await LockConfigurationReleaseForMutationAsync(
            target.DraftConfigurationReleaseId.Value, cancellationToken);
        var release = await db.ConfigurationReleases.SingleOrDefaultAsync(
            item => item.Id == target.DraftConfigurationReleaseId && item.OrganizationId == organizationId,
            cancellationToken);
        if (release is null) return null;
        var sourceResources = await LoadResourcesAsync(source, tracked: true, cancellationToken);
        var targetResources = await LoadResourcesAsync(target, tracked: true, cancellationToken);
        var menuIds = upgrade.MenuChanges.Select(item => item.MenuItemId).ToArray();
        var menus = await db.MenuItems.Include(item => item.Menu).Include(item => item.ProductOptions)
            .Where(item => menuIds.Contains(item.Id)).OrderBy(item => item.Id).ToArrayAsync(cancellationToken);
        var endpointIds = upgrade.EndpointTargets.Select(item => item.KioskExecutionEndpointId).ToArray();
        foreach (var endpointId in endpointIds.Order())
            await LockExecutionEndpointForMutationAsync(endpointId, cancellationToken);
        var endpoints = await db.KioskExecutionEndpoints.Include(item => item.Kiosk)
            .Where(item => endpointIds.Contains(item.Id)).OrderBy(item => item.Id).ToArrayAsync(cancellationToken);
        var activeDeployments = await LoadActiveDeploymentObservationsAsync(endpoints, cancellationToken);
        return new ProductionPackageUpgradeMutationState(upgrade, source, target, release,
            sourceResources, targetResources, menus, endpoints, activeDeployments);
    }

    public Task<ProductionPackageUpgrade?> GetAsync(Guid organizationId, Guid sourceInstallationId,
        Guid upgradeId, bool tracked, CancellationToken cancellationToken) => UpgradeQuery(tracked)
        .SingleOrDefaultAsync(item => item.OrganizationId == organizationId &&
                                      item.SourceInstallationId == sourceInstallationId && item.Id == upgradeId,
            cancellationToken);

    public Task<ProductionPackageUpgrade?> FindByIdempotencyKeyAsync(Guid organizationId,
        string idempotencyKey, CancellationToken cancellationToken) => UpgradeQuery(tracked: false)
        .SingleOrDefaultAsync(item => item.OrganizationId == organizationId &&
                                      item.IdempotencyKey == idempotencyKey, cancellationToken);

    public Task<ProductionPackageUpgrade?> FindActiveBySourceInstallationAsync(Guid organizationId,
        Guid sourceInstallationId, CancellationToken cancellationToken) => UpgradeQuery(tracked: false)
        .Where(item => item.OrganizationId == organizationId &&
                       item.SourceInstallationId == sourceInstallationId &&
                       (item.Status == ProductionPackageUpgradeStatus.Materializing ||
                        item.Status == ProductionPackageUpgradeStatus.ReadyForReview ||
                        item.Status == ProductionPackageUpgradeStatus.Completed ||
                        item.Status == ProductionPackageUpgradeStatus.RollbackPending))
        .OrderByDescending(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken);

    public Task<int> CountAsync(Guid organizationId, Guid sourceInstallationId,
        ProductionPackageUpgradeStatus? status, CancellationToken cancellationToken) =>
        Filter(organizationId, sourceInstallationId, status).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductionPackageUpgrade>> ListAsync(Guid organizationId,
        Guid sourceInstallationId, ProductionPackageUpgradeStatus? status, int pageNumber, int pageSize,
        CancellationToken cancellationToken) => await Filter(organizationId, sourceInstallationId, status)
        .OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id)
        .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<string>> ListConflictingProductCodesAsync(
        Guid organizationId, Guid? storeId, Guid? kioskId, IReadOnlyCollection<string> codes,
        IReadOnlyCollection<Guid> excludedProductIds, CancellationToken cancellationToken) =>
        await db.Products.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.StoreId == storeId &&
                           item.KioskId == kioskId && codes.Contains(item.Code) &&
                           !excludedProductIds.Contains(item.Id))
            .Select(item => item.Code).Distinct().OrderBy(code => code).ToArrayAsync(cancellationToken);

    public async Task<ProductionPackageUpgradeInsertResult> InsertOrGetAsync(
        ProductionPackageUpgrade upgrade, CancellationToken cancellationToken)
    {
        var entry = db.ProductionPackageUpgrades.Add(upgrade);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return new ProductionPackageUpgradeInsertResult(true, upgrade);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
               { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            entry.State = EntityState.Detached;
            var existing = await FindByIdempotencyKeyAsync(
                upgrade.OrganizationId, upgrade.IdempotencyKey, cancellationToken);
            if (existing is null)
                throw new Domain.Common.DomainRuleException(
                    "Another active upgrade already owns this source installation.");
            return new ProductionPackageUpgradeInsertResult(false, existing);
        }
    }

    public async Task<ProductionPackageUpgrade?> AttachTargetInstallationAsync(
        Guid organizationId, Guid upgradeId, Guid targetInstallationId, CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        var upgrade = await UpgradeQuery(tracked: true).SingleOrDefaultAsync(
            item => item.OrganizationId == organizationId && item.Id == upgradeId, cancellationToken);
        if (upgrade is null) return null;
        if (upgrade.TargetInstallationId == targetInstallationId &&
            upgrade.Status is ProductionPackageUpgradeStatus.ReadyForReview or
                ProductionPackageUpgradeStatus.Completed or
                ProductionPackageUpgradeStatus.RollbackPending or
                ProductionPackageUpgradeStatus.RolledBack)
            return upgrade;
        upgrade.AttachTargetInstallation(targetInstallationId, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return upgrade;
    }

    public async Task<ProductionPackageUpgrade?> ResumeFailedAsync(
        Guid organizationId, Guid upgradeId, CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        var upgrade = await UpgradeQuery(tracked: true).SingleOrDefaultAsync(
            item => item.OrganizationId == organizationId && item.Id == upgradeId, cancellationToken);
        if (upgrade is null) return null;
        upgrade.ResumeMaterialization(DateTimeOffset.UtcNow);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
               { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.ChangeTracker.Clear();
            throw new Domain.Common.DomainRuleException(
                "Another active upgrade already owns this source installation.");
        }
        return upgrade;
    }

    public async Task SoftDeleteAbandonedTargetRootsAsync(
        Guid organizationId, Guid targetInstallationId, Guid actorId, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var rows = await db.ProductionPackageMaterializations.AsNoTracking()
            .Where(item => item.InstallationId == targetInstallationId &&
                           item.Installation.OrganizationId == organizationId &&
                           (item.ResourceKind == ProductionPackageResourceKind.Product ||
                            item.ResourceKind == ProductionPackageResourceKind.RobotProgram ||
                            item.ResourceKind == ProductionPackageResourceKind.ConfigurationRelease))
            .Select(item => new { item.ResourceKind, item.TargetKey })
            .ToArrayAsync(cancellationToken);

        var targetKeys = rows.Select(item => item.TargetKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sharedRows = await db.ProductionPackageMaterializations.AsNoTracking()
            .Where(item => item.InstallationId != targetInstallationId &&
                           item.Installation.OrganizationId == organizationId &&
                           targetKeys.Contains(item.TargetKey) &&
                           (item.Installation.Status == ProductionPackageInstallationStatus.Installed ||
                            item.Installation.Status == ProductionPackageInstallationStatus.Superseded))
            .Select(item => new { item.ResourceKind, item.TargetKey })
            .Distinct()
            .ToArrayAsync(cancellationToken);
        var shared = sharedRows.Select(item => (item.ResourceKind, item.TargetKey))
            .ToHashSet();

        var productIds = Ids(ProductionPackageResourceKind.Product);
        var programIds = Ids(ProductionPackageResourceKind.RobotProgram);
        var releaseIds = Ids(ProductionPackageResourceKind.ConfigurationRelease);
        var products = await db.Products.IgnoreQueryFilters().Where(item =>
            productIds.Contains(item.Id) && item.OrganizationId == organizationId).ToArrayAsync(cancellationToken);
        var programs = await db.RobotPrograms.IgnoreQueryFilters().Where(item =>
            programIds.Contains(item.Id) && item.OrganizationId == organizationId).ToArrayAsync(cancellationToken);
        var releases = await db.ConfigurationReleases.IgnoreQueryFilters().Where(item =>
            releaseIds.Contains(item.Id) && item.OrganizationId == organizationId).ToArrayAsync(cancellationToken);

        foreach (var target in products.Cast<Domain.Common.BusinessEntity>()
                     .Concat(programs).Concat(releases))
        {
            target.DeletedAt = now;
            target.DeletedByAccountId = actorId;
            target.UpdatedAt = now;
            target.UpdatedByAccountId = actorId;
        }

        HashSet<Guid> Ids(ProductionPackageResourceKind kind) => rows
            .Where(item => item.ResourceKind == kind && !shared.Contains((kind, item.TargetKey)))
            .Select(item => Guid.TryParse(item.TargetKey, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToHashSet();
    }

    public async Task<bool> HasAbandonOperationalReferencesAsync(
        Guid organizationId, Guid targetInstallationId, CancellationToken cancellationToken)
    {
        var installation = await db.ProductionPackageInstallations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == targetInstallationId &&
                                          item.OrganizationId == organizationId, cancellationToken);
        if (installation is null)
            throw new Domain.Common.DomainRuleException("Successor installation no longer exists.");

        if (installation.DraftConfigurationReleaseId.HasValue)
        {
            var releaseId = installation.DraftConfigurationReleaseId.Value;
            var hasFullEdgeDeployment = await db.KioskConfigurationDeployments.AsNoTracking().AnyAsync(item =>
                item.OrganizationId == organizationId && item.ConfigurationReleaseId == releaseId &&
                item.Status != Domain.ProductionConfiguration.Enums.KioskConfigurationDeploymentStatus.Failed,
                cancellationToken);
            if (hasFullEdgeDeployment) return true;
            var hasControllerDeployment = await db.ControllerArtifactSetDeployments.AsNoTracking().AnyAsync(item =>
                item.OrganizationId == organizationId && item.SourceConfigurationReleaseId == releaseId &&
                item.Status != Domain.ProductionConfiguration.Enums.ControllerArtifactSetDeploymentStatus.Failed,
                cancellationToken);
            if (hasControllerDeployment) return true;
        }

        var targetProductIds = await db.ProductionPackageMaterializations.AsNoTracking()
            .Where(item => item.InstallationId == targetInstallationId &&
                           item.ResourceKind == ProductionPackageResourceKind.Product)
            .Select(item => item.TargetKey).ToArrayAsync(cancellationToken);
        var productIds = targetProductIds.Select(value => Guid.TryParse(value, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty).ToArray();
        return productIds.Length > 0 && await db.MenuItems.AsNoTracking()
            .AnyAsync(item => productIds.Contains(item.ProductId) &&
                              item.Menu.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> ListStaleMaterializingIdsAsync(
        DateTimeOffset cutoff, int batchSize, CancellationToken cancellationToken) =>
        await db.ProductionPackageUpgrades.AsNoTracking()
            .Where(item => item.Status == ProductionPackageUpgradeStatus.Materializing &&
                           (item.UpdatedAt ?? item.CreatedAt) <= cutoff)
            .OrderBy(item => item.UpdatedAt ?? item.CreatedAt).ThenBy(item => item.Id)
            .Select(item => item.Id)
            .Take(batchSize)
            .ToArrayAsync(cancellationToken);

    public async Task<bool> TryFailStaleMaterializingAsync(
        Guid upgradeId, DateTimeOffset cutoff, DateTimeOffset now, CancellationToken cancellationToken) =>
        await db.ProductionPackageUpgrades.Where(item =>
                item.Id == upgradeId && item.Status == ProductionPackageUpgradeStatus.Materializing &&
                (item.UpdatedAt ?? item.CreatedAt) <= cutoff)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.Status, ProductionPackageUpgradeStatus.Failed)
                .SetProperty(item => item.FailureCode, "UpgradeMaterializationTimedOut")
                .SetProperty(item => item.FailureMessage,
                    "Upgrade materialization exceeded the configured progress timeout.")
                .SetProperty(item => item.UpdatedAt, now), cancellationToken) == 1;

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public void ReplaceMenuItemProductOptions(MenuItem menuItem, IReadOnlyCollection<Guid> productOptionIds)
    {
        db.MenuItemProductOptions.RemoveRange(menuItem.ProductOptions);
        menuItem.ProductOptions.Clear();
        foreach (var productOptionId in productOptionIds)
            menuItem.ProductOptions.Add(new MenuItemProductOption
            {
                MenuItemId = menuItem.Id,
                ProductOptionId = productOptionId
            });
    }

    public async Task MarkFailedAsync(Guid organizationId, Guid upgradeId, string code, string message,
        CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        var upgrade = await db.ProductionPackageUpgrades.SingleOrDefaultAsync(
            item => item.OrganizationId == organizationId && item.Id == upgradeId, cancellationToken);
        if (upgrade is null || upgrade.Status != ProductionPackageUpgradeStatus.Materializing) return;
        upgrade.Fail(code, message, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<ProductionPackageUpgrade> UpgradeQuery(bool tracked)
    {
        var query = tracked ? db.ProductionPackageUpgrades : db.ProductionPackageUpgrades.AsNoTracking();
        return query
            .Include(item => item.SourceInstallation).ThenInclude(item => item.PackageVersion)
            .Include(item => item.TargetInstallation)
            .Include(item => item.TargetPackageVersion).ThenInclude(item => item.Products)
            .Include(item => item.MenuChanges).ThenInclude(item => item.OptionChanges)
            .Include(item => item.EndpointTargets).ThenInclude(item => item.RollbackAttempts)
            .Include(item => item.CatalogIdentityChanges)
            .Include(item => item.AvailabilityChanges)
            .AsSplitQuery();
    }

    private IQueryable<ProductionPackageUpgrade> Filter(Guid organizationId, Guid sourceInstallationId,
        ProductionPackageUpgradeStatus? status)
    {
        var query = UpgradeQuery(tracked: false).Where(item =>
            item.OrganizationId == organizationId && item.SourceInstallationId == sourceInstallationId);
        return status.HasValue ? query.Where(item => item.Status == status.Value) : query;
    }

    private IQueryable<ProductionPackageInstallation> InstallationQuery(bool tracked)
    {
        var query = tracked
            ? db.ProductionPackageInstallations.AsQueryable()
            : db.ProductionPackageInstallations.AsNoTracking();
        return query
            .Include(item => item.PackageVersion).ThenInclude(item => item.ProductionPackage)
            .Include(item => item.PackageVersion).ThenInclude(item => item.Products)
            .Include(item => item.PackageVersion).ThenInclude(item => item.Artifacts)
            .Include(item => item.PackageVersion).ThenInclude(item => item.Programs).ThenInclude(item => item.Slots)
            .Include(item => item.PackageVersion).ThenInclude(item => item.Routes)
            .Include(item => item.Materializations)
            .AsSplitQuery();
    }

    private async Task<ProductionPackageUpgradeResourceGraph> LoadResourcesAsync(
        ProductionPackageInstallation installation, bool tracked, CancellationToken cancellationToken)
    {
        var rows = installation.Materializations.ToArray();
        var productRows = Rows(ProductionPackageResourceKind.Product);
        var variantRows = Rows(ProductionPackageResourceKind.ProductVariant);
        var recipeRows = Rows(ProductionPackageResourceKind.Recipe);
        var optionRows = Rows(ProductionPackageResourceKind.ProductOption);
        var artifactRows = Rows(ProductionPackageResourceKind.RobotArtifact);

        IQueryable<Product> productQuery = db.Products
            .Include(item => item.ProductVariants).ThenInclude(item => item.Recipes).ThenInclude(item => item.RecipeItems)
            .Include(item => item.OptionGroups).ThenInclude(item => item.ProductOptions)
                .ThenInclude(item => item.IngredientRequirements);
        if (!tracked) productQuery = productQuery.AsNoTracking();
        var products = await productQuery.AsSplitQuery()
            .Where(item => productRows.Values.Contains(item.Id))
            .ToArrayAsync(cancellationToken);
        var productById = products.ToDictionary(item => item.Id);
        var variantsById = products.SelectMany(item => item.ProductVariants).ToDictionary(item => item.Id);
        var recipesById = products.SelectMany(item => item.ProductVariants).SelectMany(item => item.Recipes)
            .ToDictionary(item => item.Id);
        var optionsById = products.SelectMany(item => item.OptionGroups).SelectMany(item => item.ProductOptions)
            .ToDictionary(item => item.Id);
        IQueryable<RobotArtifact> artifactQuery = db.RobotArtifacts;
        if (!tracked) artifactQuery = artifactQuery.AsNoTracking();
        var artifacts = await artifactQuery.Where(item => artifactRows.Values.Contains(item.Id))
            .ToArrayAsync(cancellationToken);
        var artifactsById = artifacts.ToDictionary(item => item.Id);

        return new ProductionPackageUpgradeResourceGraph(
            Resolve(productRows, productById, "Product"),
            Resolve(variantRows, variantsById, "ProductVariant"),
            Resolve(recipeRows, recipesById, "Recipe"),
            Resolve(optionRows, optionsById, "ProductOption"),
            Resolve(artifactRows, artifactsById, "RobotArtifact"));

        Dictionary<string, Guid> Rows(ProductionPackageResourceKind kind) => rows
            .Where(item => item.ResourceKind == kind)
            .ToDictionary(item => item.SourceKey, item => Guid.TryParse(item.TargetKey, out var id)
                ? id
                : throw new InvalidOperationException($"{kind} materialization identity is invalid."),
                StringComparer.Ordinal);
    }

    private async Task<IReadOnlyCollection<MenuItem>> LoadMenuItemsAsync(Guid[] sourceProductIds,
        bool tracked, CancellationToken cancellationToken)
    {
        IQueryable<MenuItem> query = db.MenuItems
            .Include(item => item.Menu)
            .Include(item => item.ProductOptions);
        if (!tracked) query = query.AsNoTracking();
        return await query.Where(item => sourceProductIds.Contains(item.ProductId))
            .OrderBy(item => item.MenuId).ThenBy(item => item.Id).ToArrayAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<ProductionPackageUpgradeEndpointSnapshot>> LoadEndpointTargetsAsync(
        ProductionPackageInstallation installation, bool tracked, CancellationToken cancellationToken)
    {
        if (!installation.DraftConfigurationReleaseId.HasValue) return [];
        IQueryable<KioskExecutionEndpoint> query = db.KioskExecutionEndpoints.Include(item => item.Kiosk);
        if (!tracked) query = query.AsNoTracking();
        var releaseId = installation.DraftConfigurationReleaseId.Value;
        query = query.Where(item => item.Kiosk.OrganizationId == installation.OrganizationId &&
            (!installation.StoreId.HasValue || item.Kiosk.StoreId == installation.StoreId) &&
            (!installation.KioskId.HasValue || item.KioskId == installation.KioskId) &&
            ((item.ExecutionProfile == KioskExecutionProfile.FullEdge &&
              item.ActiveConfigurationReleaseId == releaseId && item.ActiveConfigurationDeploymentId != null &&
              db.KioskConfigurationDeployments.Any(deployment =>
                  deployment.Id == item.ActiveConfigurationDeploymentId &&
                  deployment.OrganizationId == installation.OrganizationId &&
                  deployment.KioskId == item.KioskId &&
                  deployment.KioskExecutionEndpointId == item.Id &&
                  deployment.ConfigurationReleaseId == releaseId &&
                  deployment.Status == KioskConfigurationDeploymentStatus.Active)) ||
             (item.ExecutionProfile == KioskExecutionProfile.LowCostController &&
              item.ActiveArtifactSetReleaseId == releaseId && item.ActiveArtifactSetDeploymentId != null &&
              db.ControllerArtifactSetDeployments.Any(deployment =>
                  deployment.Id == item.ActiveArtifactSetDeploymentId &&
                  deployment.OrganizationId == installation.OrganizationId &&
                  deployment.KioskId == item.KioskId &&
                  deployment.KioskExecutionEndpointId == item.Id &&
                  deployment.SourceConfigurationReleaseId == releaseId &&
                  deployment.Status == ControllerArtifactSetDeploymentStatus.Active))));
        var endpoints = await query.OrderBy(item => item.Id).ToArrayAsync(cancellationToken);
        return endpoints.Select(item => new ProductionPackageUpgradeEndpointSnapshot(
            item, releaseId,
            item.ExecutionProfile == KioskExecutionProfile.FullEdge
                ? item.ActiveConfigurationDeploymentId!.Value
                : item.ActiveArtifactSetDeploymentId!.Value)).ToArray();
    }

    private async Task<IReadOnlyCollection<ProductionPackageUpgradeDeploymentObservation>>
        LoadActiveDeploymentObservationsAsync(
            IReadOnlyCollection<KioskExecutionEndpoint> endpoints,
            CancellationToken cancellationToken)
    {
        var fullEdgeIds = endpoints
            .Where(item => item.ExecutionProfile == KioskExecutionProfile.FullEdge &&
                           item.ActiveConfigurationDeploymentId.HasValue)
            .Select(item => item.ActiveConfigurationDeploymentId!.Value)
            .ToArray();
        var lowCostIds = endpoints
            .Where(item => item.ExecutionProfile == KioskExecutionProfile.LowCostController &&
                           item.ActiveArtifactSetDeploymentId.HasValue)
            .Select(item => item.ActiveArtifactSetDeploymentId!.Value)
            .ToArray();

        var fullEdge = await db.KioskConfigurationDeployments.AsNoTracking()
            .Where(item => fullEdgeIds.Contains(item.Id))
            .Select(item => new ProductionPackageUpgradeDeploymentObservation(
                item.Id,
                ConfigurationDeploymentProfile.FullEdge,
                item.OrganizationId,
                item.KioskId,
                item.KioskExecutionEndpointId,
                item.ConfigurationReleaseId,
                (ConfigurationDeploymentReadStatus)item.Status))
            .ToArrayAsync(cancellationToken);
        var lowCost = await db.ControllerArtifactSetDeployments.AsNoTracking()
            .Where(item => lowCostIds.Contains(item.Id))
            .Select(item => new ProductionPackageUpgradeDeploymentObservation(
                item.Id,
                ConfigurationDeploymentProfile.LowCostController,
                item.OrganizationId,
                item.KioskId,
                item.KioskExecutionEndpointId,
                item.SourceConfigurationReleaseId,
                (ConfigurationDeploymentReadStatus)item.Status))
            .ToArrayAsync(cancellationToken);

        return fullEdge.Concat(lowCost).ToArray();
    }

    private Task LockConfigurationReleaseForMutationAsync(Guid id, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null) return Task.CompletedTask;
        return db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"ConfigurationReleases\" WHERE \"Id\" = {id} FOR UPDATE",
            cancellationToken);
    }

    private Task LockExecutionEndpointForMutationAsync(Guid id, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null) return Task.CompletedTask;
        return db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"KioskExecutionEndpoints\" WHERE \"Id\" = {id} FOR UPDATE",
            cancellationToken);
    }

    private static IReadOnlyDictionary<string, T> Resolve<T>(Dictionary<string, Guid> rows,
        IReadOnlyDictionary<Guid, T> entities, string resourceName)
    {
        var result = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var (sourceKey, id) in rows)
            result[sourceKey] = entities.TryGetValue(id, out var entity)
                ? entity
                : throw new InvalidOperationException($"{resourceName} materialization target is missing.");
        return result;
    }
}
