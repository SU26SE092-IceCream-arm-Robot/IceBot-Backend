using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Shared.Wrappers;

namespace Application.Catalog.Products.Commands;

public sealed class SetOptionGroupStatusCommandHandler(IProductStore products)
{
    public async Task<ApiResult<OptionGroupResult>> HandleAsync(SetOptionGroupStatusCommand command, CancellationToken ct = default)
    {
        var product = await products.GetProductByIdAsync(command.ProductId, cancellationToken: ct);
        if (product is null) return ApiResult<OptionGroupResult>.Fail("Product not found.", 404);
        var access = ProductManagementCommandRules.ValidateExisting<OptionGroupResult>(command.Scope, product);
        if (access is not null) return access;
        var group = await products.GetOptionGroupByIdAsync(product.Id, command.OptionGroupId, false, ct);
        if (group is null) return ApiResult<OptionGroupResult>.Fail("Option group not found.", 404);
        group.IsActive = command.IsActive;
        group.UpdatedAt = DateTimeOffset.UtcNow;
        group.UpdatedByAccountId = command.UpdatedByAccountId;
        await products.SaveChangesAsync(ct);
        return ApiResult<OptionGroupResult>.Success(ProductResultMapper.ToOptionGroupResult(group, product.Currency), "Option group status updated.");
    }
}
