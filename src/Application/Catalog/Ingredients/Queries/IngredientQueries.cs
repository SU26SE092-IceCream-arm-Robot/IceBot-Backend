using Application.Catalog.Ingredients.Results;
using Application.Identity.Tokens.Claims;

namespace Application.Catalog.Ingredients.Queries;

public sealed class ListIngredientsQuery
{
    public required CurrentUserContext UserContext { get; init; }
    public string? Search { get; init; }
    public bool? IsActive { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record GetIngredientQuery(Guid IngredientId, CurrentUserContext UserContext);
