using Application.Catalog.Abstractions;
using Domain.Catalog.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Catalog.Persistence;

public sealed class ProductStore : IProductStore
{
    private readonly IceBotDbContext _dbContext;

    public ProductStore(IceBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> CountProductsAsync(
        string? search,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool globalTemplatesOnly,
        bool isSystemAdmin,
        IReadOnlySet<Guid> allowedOrganizationIds,
        IReadOnlySet<Guid> allowedStoreIds,
        IReadOnlySet<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default)
    {
        return ApplyProductFilters(
                _dbContext.Products.WhereNotDeleted().AsNoTracking(),
                search,
                organizationId,
                storeId,
                kioskId,
                globalTemplatesOnly,
                isSystemAdmin,
                allowedOrganizationIds,
                allowedStoreIds,
                allowedKioskIds)
            .CountAsync(cancellationToken);
    }

    public Task<List<Product>> ListProductsAsync(
        string? search,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool globalTemplatesOnly,
        bool isSystemAdmin,
        IReadOnlySet<Guid> allowedOrganizationIds,
        IReadOnlySet<Guid> allowedStoreIds,
        IReadOnlySet<Guid> allowedKioskIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return ApplyProductFilters(
                _dbContext.Products.WhereNotDeleted()
                    .AsNoTracking()
                    .Include(product => product.ProductVariants)
                    .Include(product => product.OptionGroups)
                        .ThenInclude(group => group.ProductOptions)
                            .ThenInclude(option => option.IngredientRequirements),
                search,
                organizationId,
                storeId,
                kioskId,
                globalTemplatesOnly,
                isSystemAdmin,
                allowedOrganizationIds,
                allowedStoreIds,
                allowedKioskIds)
            .OrderBy(product => product.Name)
            .ThenBy(product => product.Code)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<Product?> GetProductByIdAsync(
        Guid productId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products.WhereNotDeleted()
            .Include(product => product.ProductVariants)
            .Include(product => product.OptionGroups)
                .ThenInclude(group => group.ProductOptions)
                    .ThenInclude(option => option.IngredientRequirements)
            .Where(product => product.Id == productId);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<ProductVariant?> GetProductVariantByIdAsync(
        Guid productId,
        Guid variantId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ProductVariants
            .Where(variant => variant.ProductId == productId && variant.Id == variantId);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<OptionGroup?> GetOptionGroupByIdAsync(
        Guid productId,
        long optionGroupId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.OptionGroups
            .Include(group => group.ProductOptions)
                .ThenInclude(option => option.IngredientRequirements)
            .Where(group => group.ProductId == productId && group.Id == optionGroupId);
        return (asNoTracking ? query.AsNoTracking() : query).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<ProductOption?> GetProductOptionByIdAsync(
        Guid productId,
        long optionGroupId,
        Guid productOptionId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ProductOptions
            .Include(option => option.OptionGroup)
            .Include(option => option.IngredientRequirements)
            .Where(option => option.Id == productOptionId && option.OptionGroupId == optionGroupId &&
                             option.OptionGroup.ProductId == productId);
        return (asNoTracking ? query.AsNoTracking() : query).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> ProductCodeExistsAsync(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        string code,
        Guid? excludedProductId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Products.WhereNotDeleted().AnyAsync(
            product =>
                product.OrganizationId == organizationId &&
                product.StoreId == storeId &&
                product.KioskId == kioskId &&
                product.Code == code &&
                (!excludedProductId.HasValue || product.Id != excludedProductId.Value),
            cancellationToken);
    }

    public Task<bool> ProductVariantCodeExistsAsync(
        Guid productId,
        string code,
        Guid? excludedVariantId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductVariants.AnyAsync(
            variant =>
                variant.ProductId == productId &&
                variant.Code == code &&
                (!excludedVariantId.HasValue || variant.Id != excludedVariantId.Value),
            cancellationToken);
    }

    public Task<bool> OptionGroupCodeExistsAsync(
        Guid productId,
        string code,
        long? excludedOptionGroupId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.OptionGroups.AnyAsync(group =>
            group.ProductId == productId && group.Code == code &&
            (!excludedOptionGroupId.HasValue || group.Id != excludedOptionGroupId.Value), cancellationToken);
    }

    public Task<bool> ProductOptionCodeExistsAsync(
        long optionGroupId,
        string code,
        Guid? excludedProductOptionId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductOptions.AnyAsync(option =>
            option.OptionGroupId == optionGroupId && option.Code == code && option.DeletedAt == null &&
            (!excludedProductOptionId.HasValue || option.Id != excludedProductOptionId.Value), cancellationToken);
    }

    public Task<bool> HasOtherDefaultOptionAsync(
        long optionGroupId,
        Guid? excludedProductOptionId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductOptions.AnyAsync(option =>
            option.OptionGroupId == optionGroupId && option.IsDefault && option.DeletedAt == null &&
            (!excludedProductOptionId.HasValue || option.Id != excludedProductOptionId.Value), cancellationToken);
    }

    public Task<bool> IsOptionGroupReferencedByMenuItemsAsync(long optionGroupId, CancellationToken cancellationToken = default)
    {
        return _dbContext.MenuItemProductOptions.AnyAsync(membership =>
            _dbContext.ProductOptions.Any(option => option.Id == membership.ProductOptionId && option.OptionGroupId == optionGroupId),
            cancellationToken);
    }

    public Task<bool> IsProductOptionReferencedByMenuItemsAsync(Guid productOptionId, CancellationToken cancellationToken = default)
    {
        return _dbContext.MenuItemProductOptions.AnyAsync(membership => membership.ProductOptionId == productOptionId, cancellationToken);
    }

    public Task<List<Ingredient>> ListIngredientsByIdsAsync(IReadOnlyCollection<Guid> ingredientIds, CancellationToken cancellationToken = default) =>
        _dbContext.Ingredients.Where(ingredient => ingredientIds.Contains(ingredient.Id)).ToListAsync(cancellationToken);

    public Task<bool> ProductCategoryExistsAsync(long categoryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductCategories.AnyAsync(category => category.Id == categoryId, cancellationToken);
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

    public async Task AddProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _dbContext.Products.AddAsync(product, cancellationToken);
    }

    public async Task AddProductVariantAsync(ProductVariant variant, CancellationToken cancellationToken = default)
    {
        await _dbContext.ProductVariants.AddAsync(variant, cancellationToken);
    }

    public Task AddOptionGroupAsync(OptionGroup optionGroup, CancellationToken cancellationToken = default) =>
        _dbContext.OptionGroups.AddAsync(optionGroup, cancellationToken).AsTask();

    public Task AddProductOptionAsync(ProductOption productOption, CancellationToken cancellationToken = default) =>
        _dbContext.ProductOptions.AddAsync(productOption, cancellationToken).AsTask();

    public void RemoveOptionGroup(OptionGroup optionGroup)
    {
        _dbContext.ProductOptionIngredientRequirements.RemoveRange(
            optionGroup.ProductOptions.SelectMany(option => option.IngredientRequirements));
        _dbContext.ProductOptions.RemoveRange(optionGroup.ProductOptions);
        _dbContext.OptionGroups.Remove(optionGroup);
    }

    public void RemoveProductOption(ProductOption productOption)
    {
        _dbContext.ProductOptionIngredientRequirements.RemoveRange(productOption.IngredientRequirements);
        _dbContext.ProductOptions.Remove(productOption);
    }

    public void ReplaceProductOptionIngredientRequirements(
        ProductOption option,
        IReadOnlyCollection<ProductOptionIngredientRequirement> replacements)
    {
        _dbContext.ProductOptionIngredientRequirements.RemoveRange(option.IngredientRequirements);
        option.IngredientRequirements.Clear();
        foreach (var replacement in replacements)
        {
            option.IngredientRequirements.Add(replacement);
        }
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Product> ApplyProductFilters(
        IQueryable<Product> query,
        string? search,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool globalTemplatesOnly,
        bool isSystemAdmin,
        IReadOnlySet<Guid> allowedOrganizationIds,
        IReadOnlySet<Guid> allowedStoreIds,
        IReadOnlySet<Guid> allowedKioskIds)
    {
        if (globalTemplatesOnly)
        {
            query = query.Where(product =>
                product.ScopeType == Domain.Tenants.Enums.TenantScopeType.Global &&
                product.OrganizationId == null && product.StoreId == null && product.KioskId == null);
        }

        if (!isSystemAdmin)
        {
            var allowedOrgIds = allowedOrganizationIds.ToArray();
            var allowedStoreScopeIds = allowedStoreIds.ToArray();
            var allowedKioskScopeIds = allowedKioskIds.ToArray();

            if (allowedOrgIds.Length == 0 && allowedStoreScopeIds.Length == 0 && allowedKioskScopeIds.Length == 0)
            {
                return query.Where(_ => false);
            }

            query = query.Where(product =>
                (product.OrganizationId != null && allowedOrgIds.Contains(product.OrganizationId.Value)) ||
                (product.StoreId != null && allowedStoreScopeIds.Contains(product.StoreId.Value)) ||
                (product.KioskId != null && allowedKioskScopeIds.Contains(product.KioskId.Value))
            );
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(product =>
                EF.Functions.ILike(product.Code, $"%{normalizedSearch}%") ||
                EF.Functions.ILike(product.Name, $"%{normalizedSearch}%") ||
                (product.DisplayName != null && EF.Functions.ILike(product.DisplayName, $"%{normalizedSearch}%")));
        }

        if (organizationId.HasValue)
        {
            query = query.Where(product => product.OrganizationId == organizationId.Value);
        }

        if (storeId.HasValue)
        {
            query = query.Where(product => product.StoreId == storeId.Value);
        }

        if (kioskId.HasValue)
        {
            query = query.Where(product => product.KioskId == kioskId.Value);
        }

        return query;
    }
}
