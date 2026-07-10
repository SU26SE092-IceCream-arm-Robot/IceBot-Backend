using Application.Catalog.Abstractions;
using Domain.Catalog.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Catalog.Persistence;

public sealed class CatalogAuthoringStore(IceBotDbContext dbContext) : ICatalogAuthoringStore
{
    public Task<List<ProductCategory>> ListProductCategoriesAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ProductCategories.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(category => category.IsActive);
        }

        return query
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .ThenBy(category => category.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<ProductCategory?> GetProductCategoryAsync(
        long categoryId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ProductCategories.Where(category => category.Id == categoryId);
        return (asNoTracking ? query.AsNoTracking() : query).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> ProductCategoryCodeExistsAsync(
        string code,
        long? excludedCategoryId = null,
        CancellationToken cancellationToken = default) =>
        dbContext.ProductCategories.AnyAsync(category =>
            category.Code == code &&
            (!excludedCategoryId.HasValue || category.Id != excludedCategoryId.Value), cancellationToken);

    public Task AddProductCategoryAsync(ProductCategory category, CancellationToken cancellationToken = default) =>
        dbContext.ProductCategories.AddAsync(category, cancellationToken).AsTask();

    public async Task<bool> IsProductCategoryReferencedAsync(long categoryId, CancellationToken cancellationToken = default) =>
        await dbContext.Products.IgnoreQueryFilters().AnyAsync(product => product.CategoryId == categoryId, cancellationToken) ||
        await dbContext.ProductCategories.IgnoreQueryFilters().AnyAsync(category => category.ParentCategoryId == categoryId, cancellationToken);

    public void RemoveProductCategory(ProductCategory category) => dbContext.ProductCategories.Remove(category);

    public Task<int> CountIngredientsAsync(string? search, bool? isActive, CancellationToken cancellationToken = default) =>
        ApplyIngredientFilters(dbContext.Ingredients.WhereNotDeleted().AsNoTracking(), search, isActive).CountAsync(cancellationToken);

    public Task<List<Ingredient>> ListIngredientsAsync(
        string? search,
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        ApplyIngredientFilters(dbContext.Ingredients.WhereNotDeleted().AsNoTracking(), search, isActive)
            .OrderBy(ingredient => ingredient.Name)
            .ThenBy(ingredient => ingredient.Code)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public Task<Ingredient?> GetIngredientAsync(
        Guid ingredientId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Ingredients.WhereNotDeleted().Where(ingredient => ingredient.Id == ingredientId);
        return (asNoTracking ? query.AsNoTracking() : query).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> IngredientCodeExistsAsync(
        string code,
        Guid? excludedIngredientId = null,
        CancellationToken cancellationToken = default) =>
        dbContext.Ingredients.WhereNotDeleted().AnyAsync(ingredient =>
            ingredient.Code == code &&
            (!excludedIngredientId.HasValue || ingredient.Id != excludedIngredientId), cancellationToken);

    public Task AddIngredientAsync(Ingredient ingredient, CancellationToken cancellationToken = default) =>
        dbContext.Ingredients.AddAsync(ingredient, cancellationToken).AsTask();

    public async Task<bool> IsIngredientReferencedAsync(Guid ingredientId, CancellationToken cancellationToken = default) =>
        await dbContext.RecipeItems.IgnoreQueryFilters().AnyAsync(item => item.IngredientId == ingredientId, cancellationToken) ||
        await dbContext.IngredientDispenserStates.IgnoreQueryFilters().AnyAsync(state => state.IngredientId == ingredientId, cancellationToken) ||
        await dbContext.StockMovements.IgnoreQueryFilters().AnyAsync(movement => movement.IngredientId == ingredientId, cancellationToken);

    public void RemoveIngredient(Ingredient ingredient) => dbContext.Ingredients.Remove(ingredient);

    public Task<Product?> GetProductForRecipeAuthoringAsync(Guid productId, CancellationToken cancellationToken = default) =>
        dbContext.Products.WhereNotDeleted().AsNoTracking().FirstOrDefaultAsync(product => product.Id == productId, cancellationToken);

    public Task<ProductVariant?> GetVariantForRecipeAuthoringAsync(
        Guid productId,
        Guid variantId,
        CancellationToken cancellationToken = default) =>
        dbContext.ProductVariants.AsNoTracking()
            .FirstOrDefaultAsync(variant => variant.Id == variantId && variant.ProductId == productId, cancellationToken);

    public Task<int> CountRecipesAsync(Guid variantId, CancellationToken cancellationToken = default) =>
        dbContext.Recipes.AsNoTracking().CountAsync(recipe => recipe.ProductVariantId == variantId, cancellationToken);

    public Task<List<Recipe>> ListRecipesAsync(
        Guid variantId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        dbContext.Recipes.AsNoTracking()
            .Include(recipe => recipe.RecipeItems)
                .ThenInclude(item => item.Ingredient)
            .Where(recipe => recipe.ProductVariantId == variantId)
            .OrderBy(recipe => recipe.Code)
            .ThenByDescending(recipe => recipe.Version)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public Task<Recipe?> GetRecipeAsync(
        Guid variantId,
        Guid recipeId,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Recipes
            .Include(recipe => recipe.RecipeItems)
                .ThenInclude(item => item.Ingredient)
            .Where(recipe => recipe.Id == recipeId && recipe.ProductVariantId == variantId);
        return (asNoTracking ? query.AsNoTracking() : query).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> GetNextRecipeVersionAsync(
        Guid variantId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var latest = await dbContext.Recipes
            .Where(recipe => recipe.ProductVariantId == variantId && recipe.Code == code)
            .MaxAsync(recipe => (int?)recipe.Version, cancellationToken);
        return (latest ?? 0) + 1;
    }

    public Task<bool> HasOtherDefaultRecipeAsync(
        Guid variantId,
        Guid? excludedRecipeId = null,
        CancellationToken cancellationToken = default) =>
        dbContext.Recipes.AnyAsync(recipe =>
            recipe.ProductVariantId == variantId && recipe.IsDefault &&
            recipe.Status != Domain.Catalog.Enums.RecipeStatus.Retired &&
            (!excludedRecipeId.HasValue || recipe.Id != excludedRecipeId), cancellationToken);

    public Task<List<Ingredient>> ListIngredientsByIdsAsync(
        IReadOnlyCollection<Guid> ingredientIds,
        CancellationToken cancellationToken = default) =>
        dbContext.Ingredients.WhereNotDeleted().AsNoTracking()
            .Where(ingredient => ingredientIds.Contains(ingredient.Id))
            .ToListAsync(cancellationToken);

    public Task<List<Recipe>> ListPublishedRecipesForProductCloneAsync(
        Guid productId,
        CancellationToken cancellationToken = default) =>
        dbContext.Recipes.AsNoTracking()
            .Include(recipe => recipe.RecipeItems)
            .Where(recipe => recipe.ProductVariant.ProductId == productId &&
                (recipe.Status == Domain.Catalog.Enums.RecipeStatus.Published ||
                 recipe.Status == Domain.Catalog.Enums.RecipeStatus.Active))
            .OrderBy(recipe => recipe.ProductVariantId)
            .ThenBy(recipe => recipe.Code)
            .ThenByDescending(recipe => recipe.Version)
            .ToListAsync(cancellationToken);

    public Task AddRecipeAsync(Recipe recipe, CancellationToken cancellationToken = default) =>
        dbContext.Recipes.AddAsync(recipe, cancellationToken).AsTask();

    public async Task<bool> AddRecipeWithNextVersionAsync(
        Recipe recipe,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({$"recipe-version:{recipe.ProductVariantId:D}"}, 0));",
            cancellationToken);
        recipe.Version = await GetNextRecipeVersionAsync(recipe.ProductVariantId, recipe.Code, cancellationToken);
        await dbContext.Recipes.AddAsync(recipe, cancellationToken);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }
    }

    public void ReplaceRecipeItems(Recipe recipe, IReadOnlyCollection<RecipeItem> replacements)
    {
        dbContext.RecipeItems.RemoveRange(recipe.RecipeItems);
        recipe.RecipeItems.Clear();
        foreach (var replacement in replacements)
        {
            recipe.RecipeItems.Add(replacement);
        }
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    public async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return false;
        }
    }

    private static IQueryable<Ingredient> ApplyIngredientFilters(IQueryable<Ingredient> query, string? search, bool? isActive)
    {
        if (isActive.HasValue)
        {
            query = query.Where(ingredient => ingredient.IsActive == isActive.Value);
        }

        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var normalized = search.Trim().ToLower();
        return query.Where(ingredient =>
            ingredient.Code.ToLower().Contains(normalized) ||
            ingredient.Name.ToLower().Contains(normalized));
    }
}
