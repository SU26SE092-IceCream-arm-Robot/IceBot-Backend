using Domain.Catalog.Entities;
using System.Text.Json;

namespace Application.Orders.PlaceOrder.Support;

internal static class RecipeSnapshotBuilder
{
    public static string BuildRecipeSnapshotJson(Recipe recipe)
    {
        return JsonSerializer.Serialize(new
        {
            recipe.Id,
            recipe.Code,
            recipe.Name,
            recipe.ProductVariantId,
            recipe.Version,
            recipe.Status,
            recipe.EstimatedDurationSeconds,
            recipe.InstructionsSchemaVersion,
            recipe.InstructionsJson,
            Ingredients = recipe.RecipeItems
                .OrderBy(item => item.StepOrder)
                .ThenBy(item => item.Id)
                .Select(item => new
                {
                    item.IngredientId,
                    IngredientCode = item.Ingredient.Code,
                    IngredientName = item.Ingredient.Name,
                    item.Quantity,
                    item.Unit,
                    item.StepOrder,
                    item.IsOptional
                })
        });
    }
}
