using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Catalog.Products.Rules;
using Application.Shared.Wrappers;
using Application.Shared.Ownership;
using Domain.Catalog.Entities;

namespace Application.Catalog.Products.Commands;

public sealed class ReplaceProductOptionIngredientRequirementsCommandHandler(
    IProductStore products,
    ITechnicalResourceMutationPolicy technicalOwnership)
{
    public async Task<ApiResult<ProductOptionResult>> HandleAsync(
        ReplaceProductOptionIngredientRequirementsCommand command,
        CancellationToken ct = default)
    {
        var product = await products.GetProductByIdAsync(command.ProductId, asNoTracking: false, ct);
        if (product is null) return ApiResult<ProductOptionResult>.Fail("Product not found.", 404);
        var access = ProductManagementCommandRules.ValidateExisting<ProductOptionResult>(command.Scope, product);
        if (access is not null) return access;

        var option = await products.GetProductOptionByIdAsync(
            product.Id, command.OptionGroupId, command.ProductOptionId, asNoTracking: false, ct);
        if (option is null) return ApiResult<ProductOptionResult>.Fail("Product option not found.", 404);
        var ownershipError = await technicalOwnership.ValidateDefinitionMutationAsync(
            TechnicalResourceKind.ProductOption, option.Id, ct);
        if (ownershipError is not null) return ApiResult<ProductOptionResult>.Fail(ownershipError, 409);

        var items = command.Request.Items;
        if (items.Count > 0 && option.ExecutionImpact != Domain.Catalog.Enums.ProductOptionExecutionImpact.ProductionAffecting)
            return ApiResult<ProductOptionResult>.Fail(
                "Ingredient execution requirements are allowed only for production-affecting options.", 409);
        if (items.Any(item => item.IngredientId == Guid.Empty || item.Quantity <= 0 ||
                              string.IsNullOrWhiteSpace(item.Unit) || string.IsNullOrWhiteSpace(item.RequiredWorkcellCapabilityCode)))
            return ApiResult<ProductOptionResult>.Fail("Each option ingredient requires an ingredient, positive quantity, unit, and workcell capability.");
        if (items.Select(item => item.IngredientId).Distinct().Count() != items.Count)
            return ApiResult<ProductOptionResult>.Fail("An option can require each ingredient only once.");

        var ingredientIds = items.Select(item => item.IngredientId).ToArray();
        var ingredients = await products.ListIngredientsByIdsAsync(ingredientIds, ct);
        if (ingredients.Count != ingredientIds.Length)
            return ApiResult<ProductOptionResult>.Fail("One or more ingredients were not found.", 404);
        if (ingredients.Any(ingredient => !ingredient.IsActive))
            return ApiResult<ProductOptionResult>.Fail("Inactive ingredients cannot be required by a product option.", 409);
        var ingredientById = ingredients.ToDictionary(ingredient => ingredient.Id);
        if (items.Any(item => !string.Equals(ingredientById[item.IngredientId].Unit, item.Unit.Trim(), StringComparison.OrdinalIgnoreCase)))
            return ApiResult<ProductOptionResult>.Fail("Option ingredient unit must match the ingredient catalog unit.", 409);

        var requirements = items.Select(item => new ProductOptionIngredientRequirement
        {
            ProductOptionId = option.Id,
            IngredientId = item.IngredientId,
            Quantity = item.Quantity,
            Unit = item.Unit.Trim(),
            RequiredWorkcellCapabilityCode = item.RequiredWorkcellCapabilityCode.Trim().ToUpperInvariant(),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByAccountId = command.UpdatedByAccountId
        }).ToArray();

        products.ReplaceProductOptionIngredientRequirements(option, requirements);
        option.UpdatedAt = DateTimeOffset.UtcNow;
        option.UpdatedByAccountId = command.UpdatedByAccountId;
        await products.SaveChangesAsync(ct);
        return ApiResult<ProductOptionResult>.Success(
            ProductResultMapper.ToProductOptionResult(option, product.Currency),
            "Product option ingredient requirements replaced.");
    }
}
