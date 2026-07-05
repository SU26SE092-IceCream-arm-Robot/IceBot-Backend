using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Results;
using Application.Catalog.Products.Rules;
using Application.Catalog.Products.Support;
using Application.Shared.Wrappers;
using Application.Tenants;
using Domain.Catalog.Entities;

namespace Application.Catalog.Products.Commands;

public sealed class CreateProductCommandHandler
{
    private readonly IProductStore _products;

    public CreateProductCommandHandler(IProductStore products)
    {
        _products = products;
    }

    public async Task<ApiResult<ProductResult>> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var request = command.Request;
        var createdByAccountId = command.CreatedByAccountId;

        var scopeType = command.Scope.IsGlobalTemplate
            ? Domain.Tenants.Enums.TenantScopeType.Global
            : TenantScopeResolver.Resolve(request.StoreId, request.KioskId);
        var accessError = ProductManagementCommandRules.ValidateCreate<ProductResult>(
            command.Scope, scopeType, request.StoreId, request.KioskId);
        if (accessError is not null)
        {
            return accessError;
        }

        if (!command.Scope.IsGlobalTemplate &&
            !await _products.TenantScopeExistsAsync(
                command.Scope.OrganizationId!.Value, request.StoreId, request.KioskId, cancellationToken))
        {
            return ApiResult<ProductResult>.Fail("Product scope does not belong to the route organization.");
        }

        if (command.Scope.IsGlobalTemplate)
        {
            request.StoreId = null;
            request.KioskId = null;
        }

        var validationError = await ProductRequestValidator.ValidateCreateRequestAsync(
            _products,
            request,
            command.Scope.IsGlobalTemplate ? null : command.Scope.OrganizationId,
            scopeType,
            cancellationToken);
        if (validationError is not null)
        {
            return ApiResult<ProductResult>.Fail(validationError);
        }

        var now = DateTimeOffset.UtcNow;
        var product = new Product
        {
            OrganizationId = command.Scope.IsGlobalTemplate ? null : command.Scope.OrganizationId,
            StoreId = request.StoreId,
            KioskId = request.KioskId,
            TemplateProductId = null,
            CategoryId = request.CategoryId,
            Code = ProductNormalizer.NormalizeCode(request.Code),
            Name = request.Name.Trim(),
            DisplayName = ProductNormalizer.TrimToNull(request.DisplayName),
            Description = ProductNormalizer.TrimToNull(request.Description),
            ProductType = ProductNormalizer.NormalizeOptionalCode(request.ProductType, "IceCream"),
            BasePrice = request.BasePrice,
            Currency = ProductNormalizer.NormalizeOptionalCode(request.Currency, "VND"),
            IsAvailable = false,
            PreparationTimeSeconds = request.PreparationTimeSeconds,
            ImageUrl = ProductNormalizer.TrimToNull(request.ImageUrl),
            ScopeType = scopeType,
            CreatedAt = now,
            CreatedByAccountId = createdByAccountId
        };

        foreach (var variantRequest in request.Variants)
        {
            product.ProductVariants.Add(ProductVariantFactory.CreateVariant(
                variantRequest, product.Id, product.Currency, now, createdByAccountId));
        }

        await _products.AddProductAsync(product, cancellationToken);
        await _products.SaveChangesAsync(cancellationToken);

        return ApiResult<ProductResult>.Success(ProductResultMapper.ToResult(product), "Product created.", 201);
    }
}
