using Application.Catalog.Abstractions;
using Application.Catalog.Ingredients.Mapping;
using Application.Catalog.Ingredients.Results;
using Application.Shared.Wrappers;

namespace Application.Catalog.Ingredients.Commands;

public sealed class SetIngredientStatusCommandHandler(ICatalogAuthoringStore catalog)
{
    public async Task<ApiResult<IngredientResult>> HandleAsync(SetIngredientStatusCommand command, CancellationToken ct = default)
    {
        var ingredient = await catalog.GetIngredientAsync(command.IngredientId, false, ct);
        if (ingredient is null) return ApiResult<IngredientResult>.Fail("Ingredient not found.", 404);
        if (ingredient.IsActive == command.IsActive)
            return ApiResult<IngredientResult>.Success(IngredientResultMapper.ToResult(ingredient), "Ingredient status is unchanged.");

        ingredient.IsActive = command.IsActive;
        ingredient.UpdatedAt = DateTimeOffset.UtcNow;
        ingredient.UpdatedByAccountId = command.ActorId;
        await catalog.SaveChangesAsync(ct);
        return ApiResult<IngredientResult>.Success(IngredientResultMapper.ToResult(ingredient), "Ingredient status updated.");
    }
}
