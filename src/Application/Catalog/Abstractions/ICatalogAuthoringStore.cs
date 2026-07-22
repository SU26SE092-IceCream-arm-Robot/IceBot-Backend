using Domain.Catalog.Entities;

namespace Application.Catalog.Abstractions;

public interface ICatalogAuthoringStore
{
    Task<List<ProductCategory>> ListProductCategoriesAsync(bool includeInactive, CancellationToken cancellationToken = default);
    Task<ProductCategory?> GetProductCategoryAsync(long categoryId, bool asNoTracking = true, CancellationToken cancellationToken = default);
    Task<bool> ProductCategoryCodeExistsAsync(string code, long? excludedCategoryId = null, CancellationToken cancellationToken = default);
    Task AddProductCategoryAsync(ProductCategory category, CancellationToken cancellationToken = default);
    Task<bool> IsProductCategoryReferencedAsync(long categoryId, CancellationToken cancellationToken = default);
    void RemoveProductCategory(ProductCategory category);

    Task<int> CountIngredientsAsync(string? search, bool? isActive, CancellationToken cancellationToken = default);
    Task<List<Ingredient>> ListIngredientsAsync(string? search, bool? isActive, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Ingredient?> GetIngredientAsync(Guid ingredientId, bool asNoTracking = true, CancellationToken cancellationToken = default);
    Task<bool> IngredientCodeExistsAsync(string code, Guid? excludedIngredientId = null, CancellationToken cancellationToken = default);
    Task AddIngredientAsync(Ingredient ingredient, CancellationToken cancellationToken = default);
    Task<bool> IsIngredientReferencedAsync(Guid ingredientId, CancellationToken cancellationToken = default);
    void RemoveIngredient(Ingredient ingredient);

    Task<Product?> GetProductForRecipeAuthoringAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ProductVariant?> GetVariantForRecipeAuthoringAsync(Guid productId, Guid variantId, CancellationToken cancellationToken = default);
    Task<int> CountRecipesAsync(Guid variantId, CancellationToken cancellationToken = default);
    Task<List<Recipe>> ListRecipesAsync(Guid variantId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Recipe?> GetRecipeAsync(Guid variantId, Guid recipeId, bool asNoTracking = true, CancellationToken cancellationToken = default);
    Task<int> GetNextRecipeVersionAsync(Guid variantId, string code, CancellationToken cancellationToken = default);
    Task<bool> HasOtherDefaultRecipeAsync(Guid variantId, Guid? excludedRecipeId = null, CancellationToken cancellationToken = default);
    Task<List<Ingredient>> ListIngredientsByIdsAsync(IReadOnlyCollection<Guid> ingredientIds, CancellationToken cancellationToken = default);
    Task<List<Recipe>> ListPublishedRecipesForProductCloneAsync(Guid productId, CancellationToken cancellationToken = default);
    Task AddRecipeAsync(Recipe recipe, CancellationToken cancellationToken = default);
    Task<bool> AddRecipeWithNextVersionAsync(Recipe recipe, CancellationToken cancellationToken = default);
    void ReplaceRecipeItems(Recipe recipe, IReadOnlyCollection<RecipeItem> replacements);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken = default);
}
