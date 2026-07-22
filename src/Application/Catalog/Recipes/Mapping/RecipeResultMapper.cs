using Application.Catalog.Recipes.Results;
using Domain.Catalog.Entities;

namespace Application.Catalog.Recipes.Mapping;

internal static class RecipeResultMapper
{
    public static RecipeResult ToResult(Recipe recipe) => new()
    {
        Id = recipe.Id,
        ProductVariantId = recipe.ProductVariantId,
        TemplateRecipeId = recipe.TemplateRecipeId,
        Code = recipe.Code,
        Name = recipe.Name,
        Version = recipe.Version,
        Status = recipe.Status,
        IsDefault = recipe.IsDefault,
        YieldQuantity = recipe.YieldQuantity,
        Unit = recipe.Unit,
        EstimatedDurationSeconds = recipe.EstimatedDurationSeconds,
        EffectiveFrom = recipe.EffectiveFrom,
        EffectiveTo = recipe.EffectiveTo,
        CreatedAt = recipe.CreatedAt,
        UpdatedAt = recipe.UpdatedAt,
        Items = recipe.RecipeItems
            .OrderBy(item => item.StepOrder)
            .Select(item => new RecipeItemResult
            {
                Id = item.Id,
                IngredientId = item.IngredientId,
                IngredientCode = item.Ingredient?.Code ?? string.Empty,
                IngredientName = item.Ingredient?.Name ?? string.Empty,
                Quantity = item.Quantity,
                Unit = item.Unit,
                DisplayOrder = item.StepOrder,
                IsOptional = item.IsOptional,
                Notes = item.Notes
            })
            .ToList()
    };
}
