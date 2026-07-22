using Application.Catalog.Abstractions;
using Application.Catalog.Recipes.Mapping;
using Application.Catalog.Recipes.Results;
using Application.Catalog.Recipes.Rules;
using Application.Shared.Wrappers;

namespace Application.Catalog.Recipes.Queries;

public sealed class GetRecipeQueryHandler(ICatalogAuthoringStore catalog)
{
    public async Task<ApiResult<RecipeResult>> HandleAsync(GetRecipeQuery query, CancellationToken ct = default)
    {
        var (_, variant, error) = await RecipeAuthoringRules.ResolveAsync<RecipeResult>(
            catalog, query.Scope, query.ProductId, query.VariantId, ct);
        if (error is not null) return error;

        var recipe = await catalog.GetRecipeAsync(variant!.Id, query.RecipeId, cancellationToken: ct);
        return recipe is null
            ? ApiResult<RecipeResult>.Fail("Recipe not found.", 404)
            : ApiResult<RecipeResult>.Success(RecipeResultMapper.ToResult(recipe));
    }
}
