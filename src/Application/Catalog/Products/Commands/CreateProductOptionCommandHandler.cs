using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Catalog.Products.Rules;
using Application.Catalog.Products.Support;
using Application.Shared.Wrappers;
using Domain.Catalog.Entities;

namespace Application.Catalog.Products.Commands;

public sealed class CreateProductOptionCommandHandler(IProductStore products)
{
    public async Task<ApiResult<ProductOptionResult>> HandleAsync(CreateProductOptionCommand command, CancellationToken ct = default)
    {
        var product = await products.GetProductByIdAsync(command.ProductId, cancellationToken: ct);
        if (product is null) return ApiResult<ProductOptionResult>.Fail("Product not found.", 404);
        var access = ProductManagementCommandRules.ValidateExisting<ProductOptionResult>(command.Scope, product);
        if (access is not null) return access;
        var group = await products.GetOptionGroupByIdAsync(product.Id, command.OptionGroupId, false, ct);
        if (group is null) return ApiResult<ProductOptionResult>.Fail("Option group not found.", 404);
        var request = command.Request;
        var error = ProductOptionRequestValidator.ValidateOption(request.Code, request.Name, request.PriceDelta);
        if (error is not null) return ApiResult<ProductOptionResult>.Fail(error);
        var code = ProductNormalizer.NormalizeCode(request.Code);
        if (await products.ProductOptionCodeExistsAsync(group.Id, code, cancellationToken: ct))
            return ApiResult<ProductOptionResult>.Fail("Product option code already exists in this group.", 409);
        if (request.IsDefault && await products.HasOtherDefaultOptionAsync(group.Id, cancellationToken: ct))
            return ApiResult<ProductOptionResult>.Fail("Option group already has a default option.", 409);

        var option = new ProductOption
        {
            OptionGroupId = group.Id,
            Code = code,
            Name = request.Name.Trim(),
            Description = ProductNormalizer.TrimToNull(request.Description),
            PriceDelta = request.PriceDelta,
            IsDefault = request.IsDefault,
            IsAvailable = false,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByAccountId = command.CreatedByAccountId
        };
        await products.AddProductOptionAsync(option, ct);
        await products.SaveChangesAsync(ct);
        return ApiResult<ProductOptionResult>.Success(ProductResultMapper.ToProductOptionResult(option, product.Currency), "Product option created.", 201);
    }
}
