using Application.Catalog.Abstractions;
using Application.Catalog.Ingredients.Mapping;
using Application.Catalog.Ingredients.Results;
using Application.Shared.Wrappers;

namespace Application.Catalog.Ingredients.Queries;

public sealed class ListIngredientsQueryHandler(ICatalogAuthoringStore catalog)
{
    public async Task<PagedResult<IngredientResult>> HandleAsync(ListIngredientsQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(query.PageNumber, 1);
        var size = Math.Clamp(query.PageSize, 1, 100);
        var count = await catalog.CountIngredientsAsync(query.Search, query.IsActive, ct);
        var ingredients = await catalog.ListIngredientsAsync(query.Search, query.IsActive, page, size, ct);
        return PagedResult<IngredientResult>.Success(
            ingredients.Select(IngredientResultMapper.ToResult), count, page, size);
    }
}
