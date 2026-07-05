using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Catalog.Products.Rules;
using Application.Catalog.Products.Support;
using Application.Shared.Wrappers;

namespace Application.Catalog.Products.Commands;

public sealed class UpdateProductOptionCommandHandler(IProductStore products)
{
    public async Task<ApiResult<ProductOptionResult>> HandleAsync(UpdateProductOptionCommand command, CancellationToken ct = default)
    {
        var product = await products.GetProductByIdAsync(command.ProductId, cancellationToken: ct);
        if (product is null) return ApiResult<ProductOptionResult>.Fail("Product not found.", 404);
        var access = ProductManagementCommandRules.ValidateExisting<ProductOptionResult>(command.Scope, product);
        if (access is not null) return access;
        var option = await products.GetProductOptionByIdAsync(product.Id, command.OptionGroupId, command.ProductOptionId, false, ct);
        if (option is null) return ApiResult<ProductOptionResult>.Fail("Product option not found.", 404);
        var request = command.Request;
        var error = ProductOptionRequestValidator.ValidateOption(request.Code, request.Name, request.PriceDelta);
        if (error is not null) return ApiResult<ProductOptionResult>.Fail(error);
        var code = ProductNormalizer.NormalizeCode(request.Code);
        if (await products.ProductOptionCodeExistsAsync(option.OptionGroupId, code, option.Id, ct))
            return ApiResult<ProductOptionResult>.Fail("Product option code already exists in this group.", 409);
        if (request.IsDefault && !option.IsDefault && await products.HasOtherDefaultOptionAsync(option.OptionGroupId, option.Id, ct))
            return ApiResult<ProductOptionResult>.Fail("Option group already has a default option.", 409);

        option.Code = code;
        option.Name = request.Name.Trim();
        option.Description = ProductNormalizer.TrimToNull(request.Description);
        option.PriceDelta = request.PriceDelta;
        option.IsDefault = request.IsDefault;
        option.DisplayOrder = request.DisplayOrder;
        option.UpdatedAt = DateTimeOffset.UtcNow;
        option.UpdatedByAccountId = command.UpdatedByAccountId;
        await products.SaveChangesAsync(ct);
        return ApiResult<ProductOptionResult>.Success(ProductResultMapper.ToProductOptionResult(option, product.Currency), "Product option updated.");
    }
}
