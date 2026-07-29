using Application.Catalog.Abstractions;
using Application.Catalog.Ingredients.Mapping;
using Application.Catalog.Ingredients.Results;
using Application.Shared.Wrappers;

namespace Application.Catalog.Ingredients.Queries;

public sealed class GetIngredientQueryHandler(ICatalogAuthoringStore catalog)
{
    public async Task<ApiResult<IngredientResult>> HandleAsync(GetIngredientQuery query, CancellationToken ct = default)
    {
        var ingredient = await catalog.GetIngredientAsync(query.IngredientId, cancellationToken: ct);
        return ingredient is null
            ? ApiResult<IngredientResult>.Fail("Ingredient not found.", 404)
            : ApiResult<IngredientResult>.Success(IngredientResultMapper.ToResult(ingredient));
    }
}
