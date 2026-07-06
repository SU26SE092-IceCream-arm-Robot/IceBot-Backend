using Application.Catalog.Abstractions;
using Application.Catalog.Recipes.Mapping;
using Application.Catalog.Recipes.Results;
using Application.Catalog.Recipes.Rules;
using Application.Shared.Wrappers;
using Domain.Common;

namespace Application.Catalog.Recipes.Commands;

public sealed class UpdateRecipeCommandHandler(ICatalogAuthoringStore catalog)
{
    public async Task<ApiResult<RecipeResult>> HandleAsync(UpdateRecipeCommand command, CancellationToken ct = default)
    {
        var (_, variant, error) = await RecipeAuthoringRules.ResolveAsync<RecipeResult>(
            catalog, command.Scope, command.ProductId, command.VariantId, ct);
        if (error is not null) return error;
        var recipe = await catalog.GetRecipeAsync(variant!.Id, command.RecipeId, false, ct);
        if (recipe is null) return ApiResult<RecipeResult>.Fail("Recipe not found.", 404);

        var request = command.Request;
        var validationError = RecipeAuthoringRules.ValidateRecipe(
            request.Name, request.YieldQuantity, request.Unit, request.EffectiveFrom, request.EffectiveTo);
        if (validationError is not null) return ApiResult<RecipeResult>.Fail(validationError);
        if (request.IsDefault && !recipe.IsDefault &&
            await catalog.HasOtherDefaultRecipeAsync(variant.Id, recipe.Id, ct))
            return ApiResult<RecipeResult>.Fail("Product variant already has a non-retired default recipe.", 409);

        try
        {
            recipe.UpdateDraft(request.Name.Trim(), request.YieldQuantity, request.Unit.Trim(),
                request.EstimatedDurationSeconds, request.EffectiveFrom, request.EffectiveTo,
                request.IsDefault, command.ActorId, DateTimeOffset.UtcNow);
            if (!await catalog.TrySaveChangesAsync(ct))
                return ApiResult<RecipeResult>.Fail("Default recipe changed concurrently. Reload and retry.", 409);
            return ApiResult<RecipeResult>.Success(RecipeResultMapper.ToResult(recipe), "Recipe draft updated.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<RecipeResult>.Fail(ex.Message, 409);
        }
    }
}
