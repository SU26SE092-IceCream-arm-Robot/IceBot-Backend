using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Catalog.Products.Rules;
using Application.Catalog.Products.Support;
using Application.Shared.Wrappers;
using Domain.Catalog.Entities;

namespace Application.Catalog.Products.Commands;

public sealed class CreateOptionGroupCommandHandler(IProductStore products)
{
    public async Task<ApiResult<OptionGroupResult>> HandleAsync(CreateOptionGroupCommand command, CancellationToken ct = default)
    {
        var product = await products.GetProductByIdAsync(command.ProductId, cancellationToken: ct);
        if (product is null) return ApiResult<OptionGroupResult>.Fail("Product not found.", 404);
        var access = ProductManagementCommandRules.ValidateExisting<OptionGroupResult>(command.Scope, product);
        if (access is not null) return access;

        var request = command.Request;
        var error = ProductOptionRequestValidator.ValidateGroup(request.Code, request.Name, request.SelectionType, request.MinSelections, request.MaxSelections, request.IsRequired);
        if (error is not null) return ApiResult<OptionGroupResult>.Fail(error);
        var code = ProductNormalizer.NormalizeCode(request.Code);
        if (await products.OptionGroupCodeExistsAsync(product.Id, code, cancellationToken: ct))
            return ApiResult<OptionGroupResult>.Fail("Option group code already exists for this product.", 409);

        var group = new OptionGroup
        {
            ProductId = product.Id,
            Code = code,
            Name = request.Name.Trim(),
            Description = ProductNormalizer.TrimToNull(request.Description),
            SelectionType = request.SelectionType,
            MinSelections = request.MinSelections,
            MaxSelections = request.MaxSelections,
            IsRequired = request.IsRequired,
            IsActive = true,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByAccountId = command.CreatedByAccountId
        };
        await products.AddOptionGroupAsync(group, ct);
        await products.SaveChangesAsync(ct);
        return ApiResult<OptionGroupResult>.Success(ProductResultMapper.ToOptionGroupResult(group, product.Currency), "Option group created.", 201);
    }
}
