using Application.Catalog.Abstractions;
using Application.Shared.Wrappers;

namespace Application.Catalog.Ingredients.Commands;

public sealed class DeleteIngredientCommandHandler(ICatalogAuthoringStore catalog)
{
    public async Task<ApiResult<bool>> HandleAsync(DeleteIngredientCommand command, CancellationToken ct = default)
    {
        var ingredient = await catalog.GetIngredientAsync(command.IngredientId, false, ct);
        if (ingredient is null) return ApiResult<bool>.Fail("Ingredient not found.", 404);
        if (await catalog.IsIngredientReferencedAsync(ingredient.Id, ct))
            return ApiResult<bool>.Fail("Ingredient is referenced by recipe or inventory data and cannot be deleted.", 409);

        catalog.RemoveIngredient(ingredient);
        await catalog.SaveChangesAsync(ct);
        return ApiResult<bool>.Success(true, "Ingredient deleted.");
    }
}
