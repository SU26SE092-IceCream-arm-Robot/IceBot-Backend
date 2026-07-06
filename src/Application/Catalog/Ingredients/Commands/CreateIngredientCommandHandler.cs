using Application.Catalog.Abstractions;
using Application.Catalog.Ingredients.Mapping;
using Application.Catalog.Ingredients.Results;
using Application.Catalog.Products.Support;
using Application.Shared.Wrappers;
using Domain.Catalog.Entities;

namespace Application.Catalog.Ingredients.Commands;

public sealed class CreateIngredientCommandHandler(ICatalogAuthoringStore catalog)
{
    public async Task<ApiResult<IngredientResult>> HandleAsync(CreateIngredientCommand command, CancellationToken ct = default)
    {
        var request = command.Request;
        var code = ProductNormalizer.NormalizeCode(request.Code);
        if (await catalog.IngredientCodeExistsAsync(code, cancellationToken: ct))
        {
            return ApiResult<IngredientResult>.Fail("Ingredient code already exists.", 409);
        }

        var now = DateTimeOffset.UtcNow;
        var ingredient = new Ingredient
        {
            Code = code,
            Name = request.Name.Trim(),
            IngredientType = request.IngredientType.Trim(),
            Unit = request.Unit.Trim(),
            Description = ProductNormalizer.TrimToNull(request.Description),
            StorageRequirement = ProductNormalizer.TrimToNull(request.StorageRequirement),
            IsPerishable = request.IsPerishable,
            IsAllergen = request.IsAllergen,
            ShelfLifeDays = request.ShelfLifeDays,
            IsActive = true,
            CreatedAt = now,
            CreatedByAccountId = command.ActorId
        };
        await catalog.AddIngredientAsync(ingredient, ct);
        await catalog.SaveChangesAsync(ct);
        return ApiResult<IngredientResult>.Success(IngredientResultMapper.ToResult(ingredient), "Ingredient created.", 201);
    }
}
