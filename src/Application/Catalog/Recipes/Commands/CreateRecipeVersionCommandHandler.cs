using Application.Catalog.Abstractions;
using Application.Catalog.Recipes.Mapping;
using Application.Catalog.Recipes.Results;
using Application.Catalog.Recipes.Rules;
using Application.Shared.Wrappers;
using Application.Shared.Ownership;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;

namespace Application.Catalog.Recipes.Commands;

public sealed class CreateRecipeVersionCommandHandler(
    ICatalogAuthoringStore catalog,
    ITechnicalResourceMutationPolicy technicalOwnership)
{
    public async Task<ApiResult<RecipeResult>> HandleAsync(CreateRecipeVersionCommand command, CancellationToken ct = default)
    {
        var (product, variant, error) = await RecipeAuthoringRules.ResolveAsync<RecipeResult>(
            catalog, command.Scope, command.ProductId, command.VariantId, ct);
        if (error is not null) return error;

        var source = await catalog.GetRecipeAsync(variant!.Id, command.SourceRecipeId, cancellationToken: ct);
        if (source is null) return ApiResult<RecipeResult>.Fail("Recipe not found.", 404);
        var ownershipError = await technicalOwnership.ValidateDefinitionMutationAsync(
            TechnicalResourceKind.Recipe, source.Id, ct);
        if (ownershipError is not null) return ApiResult<RecipeResult>.Fail(ownershipError, 409);
        if (source.Status == RecipeStatus.Draft)
            return ApiResult<RecipeResult>.Fail("Edit the existing Draft instead of creating a new version.", 409);

        var now = DateTimeOffset.UtcNow;
        var recipe = new Recipe
        {
            OrganizationId = product!.OrganizationId,
            StoreId = product.StoreId,
            KioskId = product.KioskId,
            ProductVariantId = variant.Id,
            TemplateRecipeId = source.TemplateRecipeId,
            Code = source.Code,
            Name = source.Name,
            Version = 0,
            Status = RecipeStatus.Draft,
            IsDefault = false,
            YieldQuantity = source.YieldQuantity,
            Unit = source.Unit,
            EstimatedDurationSeconds = source.EstimatedDurationSeconds,
            EffectiveFrom = source.EffectiveFrom,
            EffectiveTo = source.EffectiveTo,
            InstructionsSchemaVersion = source.InstructionsSchemaVersion,
            InstructionsJson = source.InstructionsJson,
            ScopeType = product.ScopeType,
            CreatedAt = now,
            CreatedByAccountId = command.ActorId
        };
        foreach (var sourceItem in source.RecipeItems.OrderBy(item => item.StepOrder))
        {
            recipe.RecipeItems.Add(new RecipeItem
            {
                RecipeId = recipe.Id,
                IngredientId = sourceItem.IngredientId,
                Quantity = sourceItem.Quantity,
                Unit = sourceItem.Unit,
                StepOrder = sourceItem.StepOrder,
                IsOptional = sourceItem.IsOptional,
                Notes = sourceItem.Notes,
                CreatedAt = now,
                CreatedByAccountId = command.ActorId
            });
        }

        if (!await catalog.AddRecipeWithNextVersionAsync(recipe, ct))
            return ApiResult<RecipeResult>.Fail("Recipe version changed concurrently. Retry the request.", 409);
        var created = await catalog.GetRecipeAsync(variant.Id, recipe.Id, cancellationToken: ct) ?? recipe;
        return ApiResult<RecipeResult>.Success(RecipeResultMapper.ToResult(created), "Recipe version created as Draft.", 201);
    }
}
