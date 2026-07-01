using Domain.Catalog.Entities;

namespace Application.Catalog.Abstractions;

public interface IProductStore
{
    Task<int> CountProductsAsync(
        string? search,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool globalTemplatesOnly,
        bool isSystemAdmin,
        IReadOnlySet<Guid> allowedOrganizationIds,
        IReadOnlySet<Guid> allowedStoreIds,
        IReadOnlySet<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default);

    Task<List<Product>> ListProductsAsync(
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
        CancellationToken cancellationToken = default);

    Task<Product?> GetProductByIdAsync(
        Guid productId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    Task<ProductVariant?> GetProductVariantByIdAsync(
        Guid productId,
        Guid variantId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    Task<bool> ProductCodeExistsAsync(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        string code,
        Guid? excludedProductId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ProductVariantCodeExistsAsync(
        Guid productId,
        string code,
        Guid? excludedVariantId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ProductCategoryExistsAsync(long categoryId, CancellationToken cancellationToken = default);

    Task<bool> TenantScopeExistsAsync(
        Guid organizationId,
        Guid? storeId,
        Guid? kioskId,
        CancellationToken cancellationToken = default);

    Task AddProductAsync(Product product, CancellationToken cancellationToken = default);

    Task AddProductVariantAsync(ProductVariant variant, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
