using Application.Catalog.Abstractions;
using Application.Catalog.Products.Commands;
using Application.Catalog.Recipes.Mapping;
using Application.Catalog.Recipes.Results;
using Application.Shared.Wrappers;

namespace Application.Catalog.Recipes.Queries;

public sealed class ListRecipesQueryHandler(ICatalogAuthoringStore catalog)
{
    public async Task<PagedResult<RecipeResult>> HandleAsync(ListRecipesQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(query.PageNumber, 1);
        var size = Math.Clamp(query.PageSize, 1, 100);
        var product = await catalog.GetProductForRecipeAuthoringAsync(query.ProductId, ct);
        if (product is null || ProductManagementCommandRules.ValidateExisting<RecipeResult>(query.Scope, product) is not null)
        {
            return PagedResult<RecipeResult>.Fail("Product not found.", 404, page, size);
        }

        var variant = await catalog.GetVariantForRecipeAuthoringAsync(query.ProductId, query.VariantId, ct);
        if (variant is null)
        {
            return PagedResult<RecipeResult>.Fail("Product variant not found.", 404, page, size);
        }

        var count = await catalog.CountRecipesAsync(variant.Id, ct);
        var recipes = await catalog.ListRecipesAsync(variant.Id, page, size, ct);
        return PagedResult<RecipeResult>.Success(recipes.Select(RecipeResultMapper.ToResult), count, page, size);
    }
}
