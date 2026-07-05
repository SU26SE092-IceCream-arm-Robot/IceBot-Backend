using Application.Catalog.Abstractions;
using Application.Shared.Wrappers;

namespace Application.Catalog.Products.Commands;

public sealed class DeleteProductOptionCommandHandler(IProductStore products)
{
    public async Task<ApiResult<bool>> HandleAsync(DeleteProductOptionCommand command, CancellationToken ct = default)
    {
        var product = await products.GetProductByIdAsync(command.ProductId, cancellationToken: ct);
        if (product is null) return ApiResult<bool>.Fail("Product not found.", 404);
        var access = ProductManagementCommandRules.ValidateExisting<bool>(command.Scope, product);
        if (access is not null) return access;
        var option = await products.GetProductOptionByIdAsync(product.Id, command.OptionGroupId, command.ProductOptionId, false, ct);
        if (option is null) return ApiResult<bool>.Fail("Product option not found.", 404);
        if (await products.IsProductOptionReferencedByMenuItemsAsync(option.Id, ct))
            return ApiResult<bool>.Fail("Product option is used by one or more menu items.", 409);
        products.RemoveProductOption(option);
        await products.SaveChangesAsync(ct);
        return ApiResult<bool>.Success(true, "Product option deleted.");
    }
}
