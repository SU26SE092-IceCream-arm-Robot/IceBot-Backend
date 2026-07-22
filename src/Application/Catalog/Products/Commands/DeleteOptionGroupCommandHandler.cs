using Application.Catalog.Abstractions;
using Application.Shared.Wrappers;
using Application.Shared.Ownership;

namespace Application.Catalog.Products.Commands;

public sealed class DeleteOptionGroupCommandHandler(
    IProductStore products,
    ITechnicalResourceMutationPolicy technicalOwnership)
{
    public async Task<ApiResult<bool>> HandleAsync(DeleteOptionGroupCommand command, CancellationToken ct = default)
    {
        var product = await products.GetProductByIdAsync(command.ProductId, cancellationToken: ct);
        if (product is null) return ApiResult<bool>.Fail("Product not found.", 404);
        var access = ProductManagementCommandRules.ValidateExisting<bool>(command.Scope, product);
        if (access is not null) return access;
        var group = await products.GetOptionGroupByIdAsync(product.Id, command.OptionGroupId, false, ct);
        if (group is null) return ApiResult<bool>.Fail("Option group not found.", 404);
        var productOwnershipError = await technicalOwnership.ValidateDefinitionMutationAsync(
            TechnicalResourceKind.Product, product.Id, ct);
        if (productOwnershipError is not null) return ApiResult<bool>.Fail(productOwnershipError, 409);
        foreach (var option in group.ProductOptions)
        {
            var ownershipError = await technicalOwnership.ValidateDefinitionMutationAsync(
                TechnicalResourceKind.ProductOption, option.Id, ct);
            if (ownershipError is not null) return ApiResult<bool>.Fail(ownershipError, 409);
        }
        if (await products.IsOptionGroupReferencedByMenuItemsAsync(group.Id, ct))
            return ApiResult<bool>.Fail("Option group is used by one or more menu items.", 409);
        products.RemoveOptionGroup(group);
        await products.SaveChangesAsync(ct);
        return ApiResult<bool>.Success(true, "Option group deleted.");
    }
}
