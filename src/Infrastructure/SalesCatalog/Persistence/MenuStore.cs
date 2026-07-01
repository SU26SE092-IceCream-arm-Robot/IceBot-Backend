using Domain.Devices.ExecutionEndpoints;
using Application.SalesCatalog.Abstractions;
using Domain.Catalog.Entities;
using Domain.Devices.Enums;
using Domain.SalesCatalog.Entities;
using Domain.SalesCatalog.Enums;
using Domain.ProductionConfiguration.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SalesCatalog.Persistence;

public sealed class MenuStore : IMenuStore
{
    private readonly IceBotDbContext _dbContext;

    public MenuStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Kiosk?> GetKioskByIdAsync(Guid kioskId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Kiosks
            .AsNoTracking()
            .Include(kiosk => kiosk.Store)
            .Include(kiosk => kiosk.Organization)
            .FirstOrDefaultAsync(kiosk => kiosk.Id == kioskId, cancellationToken);
    }

    public Task<int> CountMenusAsync(
        string? search,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlySet<Guid> allowedOrganizationIds,
        IReadOnlySet<Guid> allowedStoreIds,
        IReadOnlySet<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default)
    {
        return ApplyMenuFilters(
                _dbContext.Menus.AsNoTracking(),
                search,
                organizationId,
                storeId,
                kioskId,
                isSystemAdmin,
                allowedOrganizationIds,
                allowedStoreIds,
                allowedKioskIds)
            .CountAsync(cancellationToken);
    }

    public Task<List<Menu>> ListMenusAsync(
        string? search,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlySet<Guid> allowedOrganizationIds,
        IReadOnlySet<Guid> allowedStoreIds,
        IReadOnlySet<Guid> allowedKioskIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return ApplyMenuFilters(
                _dbContext.Menus
                    .AsNoTracking()
                    .Include(menu => menu.MenuItems),
                search,
                organizationId,
                storeId,
                kioskId,
                isSystemAdmin,
                allowedOrganizationIds,
                allowedStoreIds,
                allowedKioskIds)
            .OrderBy(menu => menu.DisplayOrder)
            .ThenBy(menu => menu.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Menu>> ListActiveMenusForKioskAsync(
        Guid? organizationId,
        Guid storeId,
        Guid kioskId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Menus
            .AsNoTracking()
            .Include(menu => menu.MenuItems)
                .ThenInclude(item => item.Product)
            .Include(menu => menu.MenuItems)
                .ThenInclude(item => item.ProductVariant)
            .Include(menu => menu.MenuItems)
                .ThenInclude(item => item.Recipe)
            .Where(menu =>
                menu.Status == MenuStatus.Active &&
                (menu.EffectiveFrom == null || menu.EffectiveFrom <= now) &&
                (menu.EffectiveTo == null || menu.EffectiveTo >= now) &&
                (
                    (organizationId.HasValue &&
                        menu.ScopeType == TenantScopeType.Organization &&
                        menu.OrganizationId == organizationId.Value) ||
                    (organizationId.HasValue &&
                        menu.ScopeType == TenantScopeType.Store &&
                        menu.OrganizationId == organizationId.Value &&
                        menu.StoreId == storeId) ||
                    (organizationId.HasValue &&
                        menu.ScopeType == TenantScopeType.Kiosk &&
                        menu.OrganizationId == organizationId.Value &&
                        menu.StoreId == storeId &&
                        menu.KioskId == kioskId)
                ))
            .OrderBy(menu => menu.DisplayOrder)
            .ThenBy(menu => menu.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasActiveProductionRouteAsync(
        Guid kioskId,
        Guid productVariantId,
        Guid recipeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ExecutionEndpointReadinessProjections
            .AsNoTracking()
            .AnyAsync(readiness =>
                readiness.KioskId == kioskId &&
                readiness.Readiness == ExecutionReadinessState.Ready &&
                readiness.Safety == ExecutionSafetyState.Safe &&
                readiness.KioskExecutionEndpoint.Status == KioskExecutionEndpointStatus.Active &&
                _dbContext.ConfigurationReleases.Any(release =>
                    release.Id == (readiness.KioskExecutionEndpoint.ExecutionProfile == KioskExecutionProfile.FullEdge
                        ? readiness.KioskExecutionEndpoint.ActiveConfigurationReleaseId
                        : readiness.KioskExecutionEndpoint.ActiveArtifactSetReleaseId) &&
                    release.Status == ConfigurationReleaseStatus.Published &&
                    release.ExecutionRoutes.Any(route =>
                        route.ProductVariantId == productVariantId && route.RecipeId == recipeId &&
                        route.RobotBindings.Any() &&
                        route.RobotBindings.All(binding => readiness.Capabilities.Any(capability =>
                            capability.IsAvailable && capability.CapabilityCode == binding.RequiredWorkcellCapabilityCode)))),
                cancellationToken);
    }

    public Task<Menu?> GetMenuByIdAsync(Guid menuId, bool asNoTracking = true, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Menus
            .Include(menu => menu.MenuItems)
            .Where(menu => menu.Id == menuId);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<MenuItem?> GetMenuItemByIdAsync(
        Guid menuId,
        Guid menuItemId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.MenuItems
            .Where(item => item.MenuId == menuId && item.Id == menuItemId);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Product?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(product => product.Id == productId, cancellationToken);
    }

    public Task<ProductVariant?> GetProductVariantByIdAsync(Guid productVariantId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductVariants
            .AsNoTracking()
            .FirstOrDefaultAsync(variant => variant.Id == productVariantId, cancellationToken);
    }

    public Task<Recipe?> GetRecipeByIdAsync(Guid recipeId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Recipes
            .AsNoTracking()
            .FirstOrDefaultAsync(recipe => recipe.Id == recipeId, cancellationToken);
    }

    public Task<bool> MenuCodeExistsAsync(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        string code,
        Guid? excludedMenuId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Menus.AnyAsync(
            menu =>
                menu.OrganizationId == organizationId &&
                menu.StoreId == storeId &&
                menu.KioskId == kioskId &&
                menu.Code == code &&
                (!excludedMenuId.HasValue || menu.Id != excludedMenuId.Value),
            cancellationToken);
    }

    public Task<bool> MenuItemCodeExistsAsync(
        Guid menuId,
        string code,
        Guid? excludedMenuItemId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.MenuItems.AnyAsync(
            item =>
                item.MenuId == menuId &&
                item.Code == code &&
                (!excludedMenuItemId.HasValue || item.Id != excludedMenuItemId.Value),
            cancellationToken);
    }

    public async Task<bool> TenantScopeExistsAsync(
        Guid organizationId,
        Guid? storeId,
        Guid? kioskId,
        CancellationToken cancellationToken = default)
    {
        if (!await _dbContext.Organizations.AnyAsync(x => x.Id == organizationId, cancellationToken))
        {
            return false;
        }

        if (storeId.HasValue && !await _dbContext.Stores.AnyAsync(
                x => x.Id == storeId && x.OrganizationId == organizationId, cancellationToken))
        {
            return false;
        }

        return !kioskId.HasValue || await _dbContext.Kiosks.AnyAsync(
            x => x.Id == kioskId && x.OrganizationId == organizationId &&
                 (!storeId.HasValue || x.StoreId == storeId), cancellationToken);
    }

    public Task AddMenuAsync(Menu menu, CancellationToken cancellationToken = default)
    {
        return _dbContext.Menus.AddAsync(menu, cancellationToken).AsTask();
    }

    public Task AddMenuItemAsync(MenuItem menuItem, CancellationToken cancellationToken = default)
    {
        return _dbContext.MenuItems.AddAsync(menuItem, cancellationToken).AsTask();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Menu> ApplyMenuFilters(
        IQueryable<Menu> query,
        string? search,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlySet<Guid> allowedOrganizationIds,
        IReadOnlySet<Guid> allowedStoreIds,
        IReadOnlySet<Guid> allowedKioskIds)
    {
        if (!isSystemAdmin)
        {
            var allowedOrgIds = allowedOrganizationIds.ToArray();
            var allowedStoreScopeIds = allowedStoreIds.ToArray();
            var allowedKioskScopeIds = allowedKioskIds.ToArray();

            if (allowedOrgIds.Length == 0 && allowedStoreScopeIds.Length == 0 && allowedKioskScopeIds.Length == 0)
            {
                return query.Where(_ => false);
            }

            query = query.Where(menu =>
                (menu.OrganizationId != null && allowedOrgIds.Contains(menu.OrganizationId.Value)) ||
                (menu.StoreId != null && allowedStoreScopeIds.Contains(menu.StoreId.Value)) ||
                (menu.KioskId != null && allowedKioskScopeIds.Contains(menu.KioskId.Value))
            );
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(menu =>
                EF.Functions.ILike(menu.Code, $"%{normalizedSearch}%") ||
                EF.Functions.ILike(menu.Name, $"%{normalizedSearch}%"));
        }

        if (organizationId.HasValue)
        {
            query = query.Where(menu => menu.OrganizationId == organizationId.Value);
        }

        if (storeId.HasValue)
        {
            query = query.Where(menu => menu.StoreId == storeId.Value);
        }

        if (kioskId.HasValue)
        {
            query = query.Where(menu => menu.KioskId == kioskId.Value);
        }

        return query;
    }
}
