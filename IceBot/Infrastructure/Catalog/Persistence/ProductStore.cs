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
        CancellationToken cancellationToken = default)
    {
        return ApplyProductFilters(_dbContext.Products.AsNoTracking(), search, organizationId, storeId, kioskId)
            .CountAsync(cancellationToken);
    }

    public Task<List<Product>> ListProductsAsync(
        string? search,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return ApplyProductFilters(
                _dbContext.Products
                    .AsNoTracking()
                    .Include(product => product.ProductVariants),
                search,
                organizationId,
                storeId,
                kioskId)
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
        var query = _dbContext.Products
            .Include(product => product.ProductVariants)
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

    public Task<bool> ProductCodeExistsAsync(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        string code,
        Guid? excludedProductId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Products.AnyAsync(
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

    public Task<bool> ProductCategoryExistsAsync(long categoryId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ProductCategories.AnyAsync(category => category.Id == categoryId, cancellationToken);
    }

    public async Task AddProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _dbContext.Products.AddAsync(product, cancellationToken);
    }

    public async Task AddProductVariantAsync(ProductVariant variant, CancellationToken cancellationToken = default)
    {
        await _dbContext.ProductVariants.AddAsync(variant, cancellationToken);
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
        Guid? kioskId)
    {
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
