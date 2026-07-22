using Application.Catalog.Ingredients.Results;
using Domain.Catalog.Entities;

namespace Application.Catalog.Ingredients.Mapping;

internal static class IngredientResultMapper
{
    public static IngredientResult ToResult(Ingredient ingredient) => new()
    {
        Id = ingredient.Id,
        Code = ingredient.Code,
        Name = ingredient.Name,
        IngredientType = ingredient.IngredientType,
        Unit = ingredient.Unit,
        Description = ingredient.Description,
        StorageRequirement = ingredient.StorageRequirement,
        IsPerishable = ingredient.IsPerishable,
        IsAllergen = ingredient.IsAllergen,
        ShelfLifeDays = ingredient.ShelfLifeDays,
        IsActive = ingredient.IsActive,
        CreatedAt = ingredient.CreatedAt,
        UpdatedAt = ingredient.UpdatedAt
    };
}
