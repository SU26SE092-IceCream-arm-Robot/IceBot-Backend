using Application.Catalog.Products.Commands;

namespace Application.Catalog.Recipes.Queries;

public sealed record ListRecipesQuery(
    ProductManagementCommandScope Scope,
    Guid ProductId,
    Guid VariantId,
    int PageNumber = 1,
    int PageSize = 20);

public sealed record GetRecipeQuery(
    ProductManagementCommandScope Scope,
    Guid ProductId,
    Guid VariantId,
    Guid RecipeId);
