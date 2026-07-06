using Application.Catalog.Abstractions;
using Application.Catalog.Recipes.Mapping;
using Application.Catalog.Recipes.Results;
using Application.Catalog.Recipes.Rules;
using Application.Shared.Wrappers;
using Domain.Catalog.Enums;
using Domain.Common;

namespace Application.Catalog.Recipes.Commands;

public sealed class SetRecipeStatusCommandHandler(ICatalogAuthoringStore catalog)
{
    public async Task<ApiResult<RecipeResult>> HandleAsync(SetRecipeStatusCommand command, CancellationToken ct = default)
    {
        var (_, variant, error) = await RecipeAuthoringRules.ResolveAsync<RecipeResult>(
            catalog, command.Scope, command.ProductId, command.VariantId, ct);
        if (error is not null) return error;
        var recipe = await catalog.GetRecipeAsync(variant!.Id, command.RecipeId, false, ct);
        if (recipe is null) return ApiResult<RecipeResult>.Fail("Recipe not found.", 404);
        if (command.Status is RecipeStatus.Draft || command.Status == recipe.Status)
            return command.Status == recipe.Status
                ? ApiResult<RecipeResult>.Success(RecipeResultMapper.ToResult(recipe), "Recipe status is unchanged.")
                : ApiResult<RecipeResult>.Fail("Recipe cannot transition back to Draft.", 409);
        if (command.Status == RecipeStatus.Active && recipe.IsDefault &&
            await catalog.HasOtherDefaultRecipeAsync(variant.Id, recipe.Id, ct))
            return ApiResult<RecipeResult>.Fail("Product variant already has a non-retired default recipe.", 409);

        try
        {
            var now = DateTimeOffset.UtcNow;
            switch (command.Status)
            {
                case RecipeStatus.Published:
                    recipe.Publish(command.ActorId, now);
                    break;
                case RecipeStatus.Active:
                    recipe.Activate(command.ActorId, now);
                    break;
                case RecipeStatus.Retired:
                    recipe.Retire(command.ActorId, now);
                    break;
                default:
                    return ApiResult<RecipeResult>.Fail("Unsupported recipe status transition.", 409);
            }

            if (!await catalog.TrySaveChangesAsync(ct))
                return ApiResult<RecipeResult>.Fail("Default recipe changed concurrently. Reload and retry.", 409);
            return ApiResult<RecipeResult>.Success(RecipeResultMapper.ToResult(recipe), "Recipe status updated.");
        }
        catch (DomainRuleException ex)
        {
            return ApiResult<RecipeResult>.Fail(ex.Message, 409);
        }
    }
}
