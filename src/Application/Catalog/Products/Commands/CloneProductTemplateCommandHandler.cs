using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Requests;
using Application.Catalog.Products.Results;
using Application.Catalog.Products.Rules;
using Application.Catalog.Products.Support;
using Application.Shared.Wrappers;
using Domain.Catalog.Entities;
using Domain.Tenants.Enums;

namespace Application.Catalog.Products.Commands;

public sealed class CloneProductTemplateCommandHandler
{
    private readonly IProductStore _products;

    public CloneProductTemplateCommandHandler(IProductStore products)
    {
        _products = products;
    }

    public async Task<ApiResult<ProductResult>> HandleAsync(
        CloneProductTemplateCommand command,
        CancellationToken cancellationToken = default)
    {
        var request = command.Request;
        var accessError = ProductManagementCommandRules.ValidateCreate<ProductResult>(
            command.Scope, request.ScopeType, request.StoreId, request.KioskId);
        if (accessError is not null)
        {
            return accessError;
        }

        var organizationId = command.Scope.OrganizationId!.Value;
        if (!await _products.TenantScopeExistsAsync(
                organizationId, request.StoreId, request.KioskId, cancellationToken))
        {
            return ApiResult<ProductResult>.Fail("Product scope does not belong to the route organization.");
        }

        var template = await _products.GetProductByIdAsync(request.TemplateProductId, cancellationToken: cancellationToken);
        if (template is null || template.ScopeType != TenantScopeType.Global || template.OrganizationId is not null)
        {
            return ApiResult<ProductResult>.Fail("Global product template not found.", 404);
        }

        var createRequest = new CreateProductRequest
        {
            StoreId = request.StoreId,
            KioskId = request.KioskId,
            CategoryId = template.CategoryId,
            Code = string.IsNullOrWhiteSpace(request.Code) ? template.Code : request.Code,
            Name = string.IsNullOrWhiteSpace(request.Name) ? template.Name : request.Name,
            DisplayName = template.DisplayName,
            Description = template.Description,
            ProductType = template.ProductType,
            BasePrice = template.BasePrice,
            Currency = template.Currency,
            IsAvailable = template.IsAvailable,
            PreparationTimeSeconds = template.PreparationTimeSeconds,
            ImageUrl = template.ImageUrl,
            MetadataJson = template.MetadataJson,
            ScopeType = request.ScopeType,
            Variants = template.ProductVariants.Select(variant => new UpsertProductVariantRequest
            {
                Code = variant.Code,
                Name = variant.Name,
                DisplayName = variant.DisplayName,
                Description = variant.Description,
                VariantType = variant.VariantType,
                FulfillmentType = variant.FulfillmentType,
                SizeCode = variant.SizeCode,
                BasePrice = variant.BasePrice,
                Currency = variant.Currency,
                IsAvailable = variant.IsAvailable,
                DisplayOrder = variant.DisplayOrder,
                PreparationTimeSeconds = variant.PreparationTimeSeconds,
                ImageUrl = variant.ImageUrl,
                MetadataJson = variant.MetadataJson
            }).ToList()
        };

        var validationError = await ProductRequestValidator.ValidateCreateRequestAsync(
            _products, createRequest, organizationId, cancellationToken);
        if (validationError is not null)
        {
            return ApiResult<ProductResult>.Fail(validationError);
        }

        var now = DateTimeOffset.UtcNow;
        var product = new Product
        {
            OrganizationId = organizationId,
            StoreId = request.StoreId,
            KioskId = request.KioskId,
            TemplateProductId = template.Id,
            CategoryId = template.CategoryId,
            Code = ProductNormalizer.NormalizeCode(createRequest.Code),
            Name = createRequest.Name.Trim(),
            DisplayName = createRequest.DisplayName,
            Description = createRequest.Description,
            ProductType = createRequest.ProductType,
            BasePrice = createRequest.BasePrice,
            Currency = createRequest.Currency,
            IsAvailable = createRequest.IsAvailable,
            PreparationTimeSeconds = createRequest.PreparationTimeSeconds,
            ImageUrl = createRequest.ImageUrl,
            MetadataJson = createRequest.MetadataJson,
            ScopeType = createRequest.ScopeType,
            CreatedAt = now,
            CreatedByAccountId = command.Scope.UserContext.AccountId
        };

        foreach (var variantRequest in createRequest.Variants)
        {
            product.ProductVariants.Add(ProductVariantFactory.CreateVariant(
                variantRequest, product.Id, now, command.Scope.UserContext.AccountId));
        }

        await _products.AddProductAsync(product, cancellationToken);
        await _products.SaveChangesAsync(cancellationToken);
        return ApiResult<ProductResult>.Success(ProductResultMapper.ToResult(product), "Product template cloned.", 201);
    }
}
