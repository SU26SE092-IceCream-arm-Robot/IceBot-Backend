using Application.Catalog.Abstractions;
using Application.Catalog.Recipes.Mapping;
using Application.Catalog.Recipes.Results;
using Application.Catalog.Recipes.Rules;
using Application.Shared.Wrappers;
using Domain.Catalog.Entities;
using Domain.Common;

namespace Application.Catalog.Recipes.Commands;

public sealed class ReplaceRecipeItemsCommandHandler(ICatalogAuthoringStore catalog)
{
    public async Task<ApiResult<RecipeResult>> HandleAsync(ReplaceRecipeItemsCommand command, CancellationToken ct = default)
    {
        var (_, variant, error) = await RecipeAuthoringRules.ResolveAsync<RecipeResult>(
            catalog, command.Scope, command.ProductId, command.VariantId, ct);
        if (error is not null) return error;
        var recipe = await catalog.GetRecipeAsync(variant!.Id, command.RecipeId, false, ct);
        if (recipe is null) return ApiResult<RecipeResult>.Fail("Recipe not found.", 404);

        var validationError = RecipeAuthoringRules.ValidateItems(command.Request.Items);
        if (validationError is not null) return ApiResult<RecipeResult>.Fail(validationError);
        var ingredientIds = command.Request.Items.Select(item => item.IngredientId).ToHashSet();
        var ingredients = await catalog.ListIngredientsByIdsAsync(ingredientIds, ct);
        if (ingredients.Count != ingredientIds.Count)
            return ApiResult<RecipeResult>.Fail("One or more ingredients were not found.");
        if (ingredients.Any(ingredient => !ingredient.IsActive))
            return ApiResult<RecipeResult>.Fail("Inactive ingredients cannot be added to a recipe.", 409);

        try
        {
            recipe.EnsureDraft();
            var now = DateTimeOffset.UtcNow;
            var replacements = command.Request.Items.Select(item => new RecipeItem
            {
                RecipeId = recipe.Id,
                IngredientId = item.IngredientId,
                Quantity = item.Quantity,
                Unit = item.Unit.Trim(),
                StepOrder = item.DisplayOrder,
                IsOptional = item.IsOptional,
                Notes = RecipeAuthoringRules.TrimToNull(item.Notes),
                CreatedAt = now,
                CreatedByAccountId = command.ActorId
            }).ToList();
            catalog.ReplaceRecipeItems(recipe, replacements);
            recipe.UpdatedAt = now;
            recipe.UpdatedByAccountId = command.ActorId;
            await catalog.SaveChangesAsync(ct);
            var updated = await catalog.GetRecipeAsync(variant.Id, recipe.Id, cancellationToken: ct)
                          ?? throw new InvalidOperationException("Updated recipe could not be reloaded.");
            return ApiResult<RecipeResult>.Success(RecipeResultMapper.ToResult(updated), "Recipe ingredients replaced.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<RecipeResult>.Fail(ex.Message, 409);
        }
    }
}
