using Application.Catalog.Abstractions;
using Application.Catalog.Ingredients.Mapping;
using Application.Catalog.Ingredients.Results;
using Application.Catalog.Products.Support;
using Application.Shared.Wrappers;

namespace Application.Catalog.Ingredients.Commands;

public sealed class UpdateIngredientCommandHandler(ICatalogAuthoringStore catalog)
{
    public async Task<ApiResult<IngredientResult>> HandleAsync(UpdateIngredientCommand command, CancellationToken ct = default)
    {
        var ingredient = await catalog.GetIngredientAsync(command.IngredientId, false, ct);
        if (ingredient is null)
        {
            return ApiResult<IngredientResult>.Fail("Ingredient not found.", 404);
        }

        var request = command.Request;
        ingredient.Name = request.Name.Trim();
        ingredient.IngredientType = request.IngredientType.Trim();
        ingredient.Unit = request.Unit.Trim();
        ingredient.Description = ProductNormalizer.TrimToNull(request.Description);
        ingredient.StorageRequirement = ProductNormalizer.TrimToNull(request.StorageRequirement);
        ingredient.IsPerishable = request.IsPerishable;
        ingredient.IsAllergen = request.IsAllergen;
        ingredient.ShelfLifeDays = request.ShelfLifeDays;
        ingredient.UpdatedAt = DateTimeOffset.UtcNow;
        ingredient.UpdatedByAccountId = command.ActorId;
        await catalog.SaveChangesAsync(ct);
        return ApiResult<IngredientResult>.Success(IngredientResultMapper.ToResult(ingredient), "Ingredient updated.");
    }
}
