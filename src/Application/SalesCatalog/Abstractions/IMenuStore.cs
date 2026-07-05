using Domain.Catalog.Entities;
using Domain.SalesCatalog.Entities;
using Domain.Tenants.Entities;
using Application.SalesCatalog.ReadModels;

namespace Application.SalesCatalog.Abstractions;

public interface IMenuStore
{
    Task<Kiosk?> GetKioskByIdAsync(Guid kioskId, CancellationToken cancellationToken = default);

    Task<int> CountMenusAsync(
        string? search,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        bool isSystemAdmin,
        IReadOnlySet<Guid> allowedOrganizationIds,
        IReadOnlySet<Guid> allowedStoreIds,
        IReadOnlySet<Guid> allowedKioskIds,
        CancellationToken cancellationToken = default);

    Task<List<Menu>> ListMenusAsync(
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
        CancellationToken cancellationToken = default);

    Task<List<Menu>> ListActiveMenusForKioskAsync(
        Guid? organizationId,
        Guid storeId,
        Guid kioskId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveProductionRouteAsync(
        Guid kioskId,
        Guid productVariantId,
        Guid recipeId,
        CancellationToken cancellationToken = default);

    Task<Menu?> GetMenuByIdAsync(Guid menuId, bool asNoTracking = true, CancellationToken cancellationToken = default);

    Task<MenuItem?> GetMenuItemByIdAsync(
        Guid menuId,
        Guid menuItemId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    Task<Product?> GetProductByIdAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<ProductVariant?> GetProductVariantByIdAsync(Guid productVariantId, CancellationToken cancellationToken = default);

    Task<Recipe?> GetRecipeByIdAsync(Guid recipeId, CancellationToken cancellationToken = default);

    Task<List<ProductOption>> ListProductOptionsAsync(
        Guid productId,
        IReadOnlyCollection<Guid> optionIds,
        CancellationToken cancellationToken = default);

    Task<List<MenuItemProductOptionReadModel>> ListMenuItemProductOptionsAsync(
        IReadOnlyCollection<Guid> menuItemIds,
        CancellationToken cancellationToken = default);

    Task<bool> MenuCodeExistsAsync(
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        string code,
        Guid? excludedMenuId = null,
        CancellationToken cancellationToken = default);

    Task<bool> MenuItemCodeExistsAsync(
        Guid menuId,
        string code,
        Guid? excludedMenuItemId = null,
        CancellationToken cancellationToken = default);

    Task<bool> TenantScopeExistsAsync(
        Guid organizationId,
        Guid? storeId,
        Guid? kioskId,
        CancellationToken cancellationToken = default);

    Task AddMenuAsync(Menu menu, CancellationToken cancellationToken = default);

    Task AddMenuItemAsync(MenuItem menuItem, CancellationToken cancellationToken = default);

    void ReplaceMenuItemProductOptions(
        MenuItem menuItem,
        IReadOnlyCollection<MenuItemProductOption> replacements);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
