using Domain.Devices.ExecutionEndpoints;
using Application.SalesCatalog.Abstractions;
using Domain.Catalog.Entities;
using Domain.Devices.Catalog;
using Domain.SalesCatalog.Entities;
using Domain.SalesCatalog.Enums;
using Domain.ProductionConfiguration.Enums;
using Domain.Tenants.Entities;
using Domain.Tenants.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Application.SalesCatalog.ReadModels;
using Application.SalesCatalog.Availability;
using Application.ProductionConfiguration.Routes.Support;

namespace Infrastructure.SalesCatalog.Persistence;

public sealed partial class MenuStore : IMenuStore
{
    private readonly IceBotDbContext _dbContext;

    public MenuStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Kiosk?> GetKioskByIdAsync(Guid kioskId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Kiosks.WhereNotDeleted()
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
                    .Include(menu => menu.MenuItems)
                        .ThenInclude(item => item.ProductOptions),
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
            .AsSplitQuery()
            .Include(menu => menu.MenuItems)
                .ThenInclude(item => item.Product)
                    .ThenInclude(product => product.ImageAsset)
            .Include(menu => menu.MenuItems)
                .ThenInclude(item => item.ProductVariant)
                    .ThenInclude(variant => variant.ImageAsset)
            .Include(menu => menu.MenuItems)
                .ThenInclude(item => item.Recipe)
                    .ThenInclude(recipe => recipe!.RecipeItems)
                        .ThenInclude(recipeItem => recipeItem.Ingredient)
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

    public async Task<IReadOnlyDictionary<ActiveProductionRouteOptionPolicyKey, ActiveProductionRouteOptionPolicy>>
        GetActiveProductionRouteOptionPoliciesAsync(
        Guid kioskId,
        IReadOnlyCollection<ActiveProductionRouteOptionPolicyKey> keys,
        DateTimeOffset readinessReceivedAfter,
        CancellationToken cancellationToken = default)
    {
        if (keys.Count == 0)
        {
            return new Dictionary<ActiveProductionRouteOptionPolicyKey, ActiveProductionRouteOptionPolicy>();
        }

        var requestedKeys = keys.ToHashSet();
        var routes = await ExecutionRouteAvailabilityReader.ListAsync(
            _dbContext, kioskId, keys, readinessReceivedAfter, cancellationToken);

        return routes
            .Where(candidate => requestedKeys.Contains(new ActiveProductionRouteOptionPolicyKey(
                candidate.ProductVariantId,
                candidate.RecipeId)))
            .GroupBy(candidate => new ActiveProductionRouteOptionPolicyKey(
                candidate.ProductVariantId,
                candidate.RecipeId))
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var route = group.First();
                    return new ActiveProductionRouteOptionPolicy(route.Id, route.SupportedOptionCodes);
                });
    }

    public Task<Menu?> GetMenuByIdAsync(Guid menuId, bool asNoTracking = true, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Menus
            .Include(menu => menu.MenuItems)
                .ThenInclude(item => item.ProductOptions)
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
            .Include(item => item.ProductOptions)
            .Where(item => item.MenuId == menuId && item.Id == menuItemId);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Product?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products.WhereNotDeleted()
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
            .Include(recipe => recipe.RecipeItems)
                .ThenInclude(item => item.Ingredient)
            .FirstOrDefaultAsync(recipe => recipe.Id == recipeId, cancellationToken);
    }

    public Task<Domain.Devices.Connectivity.KioskConnectivityProjection?> GetKioskConnectivityAsync(
        Guid kioskId,
        CancellationToken cancellationToken = default) =>
        _dbContext.KioskConnectivityProjections.AsNoTracking()
            .FirstOrDefaultAsync(connectivity => connectivity.KioskId == kioskId, cancellationToken);

    public Task<List<ProductOption>> ListProductOptionsAsync(
        Guid productId,
        IReadOnlyCollection<Guid> optionIds,
        CancellationToken cancellationToken = default)
    {
        if (optionIds.Count == 0)
        {
            return Task.FromResult(new List<ProductOption>());
        }

        return _dbContext.Products.WhereNotDeleted()
            .AsNoTracking()
            .Where(product => product.Id == productId)
            .SelectMany(product => product.OptionGroups)
            .SelectMany(group => group.ProductOptions)
            .Where(option => optionIds.Contains(option.Id) && option.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public Task<List<MenuItemProductOptionReadModel>> ListMenuItemProductOptionsAsync(
        IReadOnlyCollection<Guid> menuItemIds,
        CancellationToken cancellationToken = default)
    {
        if (menuItemIds.Count == 0)
        {
            return Task.FromResult(new List<MenuItemProductOptionReadModel>());
        }

        return ProjectMenuItemOptions(menuItemIds).ToListAsync(cancellationToken);
    }

    public Task<List<MenuItemOptionGroupReadModel>> ListMenuItemOptionGroupsAsync(
        IReadOnlyCollection<Guid> menuItemIds,
        CancellationToken cancellationToken = default)
    {
        if (menuItemIds.Count == 0)
        {
            return Task.FromResult(new List<MenuItemOptionGroupReadModel>());
        }

        return (from menuItem in _dbContext.MenuItems.AsNoTracking()
                join optionGroup in _dbContext.OptionGroups.AsNoTracking()
                    on menuItem.ProductId equals optionGroup.ProductId
                where menuItemIds.Contains(menuItem.Id) && optionGroup.IsActive
                select new MenuItemOptionGroupReadModel(
                    menuItem.Id,
                    optionGroup.Id,
                    optionGroup.Code,
                    optionGroup.Name,
                    optionGroup.SelectionType,
                    optionGroup.MinSelections,
                    optionGroup.MaxSelections,
                    optionGroup.IsRequired))
            .ToListAsync(cancellationToken);
    }

    private IQueryable<MenuItemProductOptionReadModel> ProjectMenuItemOptions(IReadOnlyCollection<Guid> menuItemIds)
    {
        return from membership in _dbContext.MenuItemProductOptions.AsNoTracking()
               join option in _dbContext.ProductOptions.AsNoTracking() on membership.ProductOptionId equals option.Id
               join optionGroup in _dbContext.OptionGroups.AsNoTracking() on option.OptionGroupId equals optionGroup.Id
               where menuItemIds.Contains(membership.MenuItemId) && membership.DeletedAt == null &&
                     option.DeletedAt == null && optionGroup.IsActive
               select new MenuItemProductOptionReadModel(
                   membership.MenuItemId,
                   option.Id,
                   optionGroup.Id,
                   optionGroup.Code,
                   optionGroup.Name,
                   optionGroup.SelectionType,
                   optionGroup.MinSelections,
                   optionGroup.MaxSelections,
                   optionGroup.IsRequired,
                   option.Code,
                   option.Name,
                   option.Description,
                   option.PriceDelta,
                   option.ExecutionImpact,
                   option.IsAvailable,
                   !_dbContext.ProductOptionIngredientRequirements.Any(requirement =>
                       requirement.ProductOptionId == option.Id && !requirement.Ingredient.IsActive),
                   option.IsDefault,
                   option.DisplayOrder);
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
        if (!await _dbContext.Organizations.WhereNotDeleted().AnyAsync(x => x.Id == organizationId, cancellationToken))
        {
            return false;
        }

        if (storeId.HasValue && !await _dbContext.Stores.WhereNotDeleted().AnyAsync(
                x => x.Id == storeId && x.OrganizationId == organizationId, cancellationToken))
        {
            return false;
        }

        return !kioskId.HasValue || await _dbContext.Kiosks.WhereNotDeleted().AnyAsync(
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

    public void ReplaceMenuItemProductOptions(
        MenuItem menuItem,
        IReadOnlyCollection<MenuItemProductOption> replacements)
    {
        _dbContext.MenuItemProductOptions.RemoveRange(menuItem.ProductOptions);
        menuItem.ProductOptions.Clear();
        foreach (var replacement in replacements)
        {
            menuItem.ProductOptions.Add(replacement);
        }
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
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
