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

    Task<OptionGroup?> GetOptionGroupByIdAsync(
        Guid productId,
        long optionGroupId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    Task<ProductOption?> GetProductOptionByIdAsync(
        Guid productId,
        long optionGroupId,
        Guid productOptionId,
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

    Task<bool> OptionGroupCodeExistsAsync(
        Guid productId,
        string code,
        long? excludedOptionGroupId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ProductOptionCodeExistsAsync(
        long optionGroupId,
        string code,
        Guid? excludedProductOptionId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasOtherDefaultOptionAsync(
        long optionGroupId,
        Guid? excludedProductOptionId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsOptionGroupReferencedByMenuItemsAsync(long optionGroupId, CancellationToken cancellationToken = default);

    Task<bool> IsProductOptionReferencedByMenuItemsAsync(Guid productOptionId, CancellationToken cancellationToken = default);

    Task<bool> IsProductReferencedByMenuItemsAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<bool> IsProductVariantReferencedByMenuItemsAsync(Guid productVariantId, CancellationToken cancellationToken = default);

    Task<List<Ingredient>> ListIngredientsByIdsAsync(IReadOnlyCollection<Guid> ingredientIds, CancellationToken cancellationToken = default);

    Task<bool> ProductCategoryExistsAsync(long categoryId, CancellationToken cancellationToken = default);

    Task<bool> TenantScopeExistsAsync(
        Guid organizationId,
        Guid? storeId,
        Guid? kioskId,
        CancellationToken cancellationToken = default);

    Task AddProductAsync(Product product, CancellationToken cancellationToken = default);

    Task AddProductVariantAsync(ProductVariant variant, CancellationToken cancellationToken = default);

    Task AddOptionGroupAsync(OptionGroup optionGroup, CancellationToken cancellationToken = default);

    Task AddProductOptionAsync(ProductOption productOption, CancellationToken cancellationToken = default);

    void RemoveOptionGroup(OptionGroup optionGroup);

    void RemoveProductOption(ProductOption productOption);

    void ReplaceProductOptionIngredientRequirements(ProductOption option, IReadOnlyCollection<ProductOptionIngredientRequirement> replacements);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
