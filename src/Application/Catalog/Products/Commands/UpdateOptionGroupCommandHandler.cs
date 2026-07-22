using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Catalog.Products.Rules;
using Application.Catalog.Products.Support;
using Application.Shared.Ownership;
using Application.Shared.Wrappers;

namespace Application.Catalog.Products.Commands;

public sealed class UpdateOptionGroupCommandHandler(
    IProductStore products,
    ITechnicalResourceMutationPolicy technicalOwnership)
{
    public async Task<ApiResult<OptionGroupResult>> HandleAsync(UpdateOptionGroupCommand command, CancellationToken ct = default)
    {
        var product = await products.GetProductByIdAsync(command.ProductId, cancellationToken: ct);
        if (product is null) return ApiResult<OptionGroupResult>.Fail("Product not found.", 404);
        var access = ProductManagementCommandRules.ValidateExisting<OptionGroupResult>(command.Scope, product);
        if (access is not null) return access;
        var group = await products.GetOptionGroupByIdAsync(product.Id, command.OptionGroupId, false, ct);
        if (group is null) return ApiResult<OptionGroupResult>.Fail("Option group not found.", 404);

        var request = command.Request;
        var error = ProductOptionRequestValidator.ValidateGroup(request.Code, request.Name, request.SelectionType, request.MinSelections, request.MaxSelections, request.IsRequired);
        if (error is not null) return ApiResult<OptionGroupResult>.Fail(error);
        var code = ProductNormalizer.NormalizeCode(request.Code);
        if (!string.Equals(code, group.Code, StringComparison.Ordinal) ||
            request.SelectionType != group.SelectionType ||
            request.MinSelections != group.MinSelections ||
            request.MaxSelections != group.MaxSelections ||
            request.IsRequired != group.IsRequired)
        {
            var ownershipError = await technicalOwnership.ValidateDefinitionMutationAsync(
                TechnicalResourceKind.Product, product.Id, ct);
            if (ownershipError is not null) return ApiResult<OptionGroupResult>.Fail(ownershipError, 409);
        }
        if (await products.OptionGroupCodeExistsAsync(product.Id, code, group.Id, ct))
            return ApiResult<OptionGroupResult>.Fail("Option group code already exists for this product.", 409);

        group.Code = code;
        group.Name = request.Name.Trim();
        group.Description = ProductNormalizer.TrimToNull(request.Description);
        group.SelectionType = request.SelectionType;
        group.MinSelections = request.MinSelections;
        group.MaxSelections = request.MaxSelections;
        group.IsRequired = request.IsRequired;
        group.DisplayOrder = request.DisplayOrder;
        group.UpdatedAt = DateTimeOffset.UtcNow;
        group.UpdatedByAccountId = command.UpdatedByAccountId;
        await products.SaveChangesAsync(ct);
        return ApiResult<OptionGroupResult>.Success(ProductResultMapper.ToOptionGroupResult(group, product.Currency), "Option group updated.");
    }
}
