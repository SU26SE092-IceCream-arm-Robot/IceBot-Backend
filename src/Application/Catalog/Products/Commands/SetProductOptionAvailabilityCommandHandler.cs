using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Shared.Wrappers;

namespace Application.Catalog.Products.Commands;

public sealed class SetProductOptionAvailabilityCommandHandler(IProductStore products)
{
    public async Task<ApiResult<ProductOptionResult>> HandleAsync(SetProductOptionAvailabilityCommand command, CancellationToken ct = default)
    {
        var product = await products.GetProductByIdAsync(command.ProductId, cancellationToken: ct);
        if (product is null) return ApiResult<ProductOptionResult>.Fail("Product not found.", 404);
        var access = ProductManagementCommandRules.ValidateExisting<ProductOptionResult>(command.Scope, product);
        if (access is not null) return access;
        var option = await products.GetProductOptionByIdAsync(product.Id, command.OptionGroupId, command.ProductOptionId, false, ct);
        if (option is null) return ApiResult<ProductOptionResult>.Fail("Product option not found.", 404);
        option.IsAvailable = command.IsAvailable;
        option.UpdatedAt = DateTimeOffset.UtcNow;
        option.UpdatedByAccountId = command.UpdatedByAccountId;
        await products.SaveChangesAsync(ct);
        return ApiResult<ProductOptionResult>.Success(ProductResultMapper.ToProductOptionResult(option, product.Currency), "Product option availability updated.");
    }
}
