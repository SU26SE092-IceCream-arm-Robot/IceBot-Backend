using Application.Catalog.Abstractions;
using Application.Catalog.Recipes.Mapping;
using Application.Catalog.Recipes.Results;
using Application.Catalog.Recipes.Rules;
using Application.Shared.Wrappers;
using Domain.Catalog.Entities;
using Domain.Catalog.Enums;

namespace Application.Catalog.Recipes.Commands;

public sealed class CreateRecipeCommandHandler(ICatalogAuthoringStore catalog)
{
    public async Task<ApiResult<RecipeResult>> HandleAsync(CreateRecipeCommand command, CancellationToken ct = default)
    {
        var (product, variant, error) = await RecipeAuthoringRules.ResolveAsync<RecipeResult>(
            catalog, command.Scope, command.ProductId, command.VariantId, ct);
        if (error is not null) return error;

        var request = command.Request;
        var validationError = RecipeAuthoringRules.ValidateRecipe(
            request.Name, request.YieldQuantity, request.Unit, request.EffectiveFrom, request.EffectiveTo);
        if (validationError is not null) return ApiResult<RecipeResult>.Fail(validationError);
        if (request.IsDefault && await catalog.HasOtherDefaultRecipeAsync(variant!.Id, cancellationToken: ct))
            return ApiResult<RecipeResult>.Fail("Product variant already has a non-retired default recipe.", 409);

        var code = RecipeAuthoringRules.NormalizeCode(request.Code);
        var now = DateTimeOffset.UtcNow;
        var recipe = new Recipe
        {
            OrganizationId = product!.OrganizationId,
            StoreId = product.StoreId,
            KioskId = product.KioskId,
            ProductVariantId = variant!.Id,
            Code = code,
            Name = request.Name.Trim(),
            Version = 0,
            Status = RecipeStatus.Draft,
            IsDefault = request.IsDefault,
            YieldQuantity = request.YieldQuantity,
            Unit = request.Unit.Trim(),
            EstimatedDurationSeconds = request.EstimatedDurationSeconds,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            ScopeType = product.ScopeType,
            CreatedAt = now,
            CreatedByAccountId = command.ActorId
        };
        if (!await catalog.AddRecipeWithNextVersionAsync(recipe, ct))
            return ApiResult<RecipeResult>.Fail("Recipe version or default recipe changed concurrently. Retry the request.", 409);
        return ApiResult<RecipeResult>.Success(RecipeResultMapper.ToResult(recipe), "Recipe draft created.", 201);
    }
}
