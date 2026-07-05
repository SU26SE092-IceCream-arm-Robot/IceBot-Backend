using Application.Catalog.Abstractions;
using Application.Catalog.Products.Mapping;
using Application.Catalog.Products.Requests;
using Application.Catalog.Products.Results;
using Application.Catalog.Products.Rules;
using Application.Catalog.Products.Support;
using Application.Shared.Wrappers;
using Domain.Catalog.Entities;
using Domain.Tenants.Enums;
using Application.Tenants;

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
        var scopeType = TenantScopeResolver.Resolve(request.StoreId, request.KioskId);
        var accessError = ProductManagementCommandRules.ValidateCreate<ProductResult>(
            command.Scope, scopeType, request.StoreId, request.KioskId);
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
            PreparationTimeSeconds = template.PreparationTimeSeconds,
            ImageUrl = template.ImageUrl,
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
                DisplayOrder = variant.DisplayOrder,
                PreparationTimeSeconds = variant.PreparationTimeSeconds,
                ImageUrl = variant.ImageUrl
            }).ToList()
        };

        var validationError = await ProductRequestValidator.ValidateCreateRequestAsync(
            _products, createRequest, organizationId, scopeType, cancellationToken);
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
            IsAvailable = false,
            PreparationTimeSeconds = createRequest.PreparationTimeSeconds,
            ImageUrl = createRequest.ImageUrl,
            MetadataJson = template.MetadataJson,
            ScopeType = scopeType,
            CreatedAt = now,
            CreatedByAccountId = command.Scope.UserContext.AccountId
        };

        var templateVariantsByCode = template.ProductVariants.ToDictionary(
            variant => variant.Code,
            StringComparer.OrdinalIgnoreCase);
        foreach (var variantRequest in createRequest.Variants)
        {
            var sourceVariant = templateVariantsByCode[variantRequest.Code];
            product.ProductVariants.Add(ProductVariantFactory.CreateVariant(
                variantRequest,
                product.Id,
                product.Currency,
                now,
                command.Scope.UserContext.AccountId,
                sourceVariant.MetadataJson));
        }

        foreach (var sourceGroup in template.OptionGroups.OrderBy(group => group.DisplayOrder))
        {
            var clonedGroup = new OptionGroup
            {
                ProductId = product.Id,
                Code = sourceGroup.Code,
                Name = sourceGroup.Name,
                Description = sourceGroup.Description,
                SelectionType = sourceGroup.SelectionType,
                MinSelections = sourceGroup.MinSelections,
                MaxSelections = sourceGroup.MaxSelections,
                IsRequired = sourceGroup.IsRequired,
                IsActive = sourceGroup.IsActive,
                DisplayOrder = sourceGroup.DisplayOrder,
                CreatedAt = now,
                CreatedByAccountId = command.Scope.UserContext.AccountId
            };
            foreach (var sourceOption in sourceGroup.ProductOptions.Where(option => option.DeletedAt == null))
            {
                clonedGroup.ProductOptions.Add(new ProductOption
                {
                    OptionGroupId = clonedGroup.Id,
                    TemplateProductOptionId = sourceOption.Id,
                    Code = sourceOption.Code,
                    Name = sourceOption.Name,
                    Description = sourceOption.Description,
                    PriceDelta = sourceOption.PriceDelta,
                    IsDefault = sourceOption.IsDefault,
                    IsAvailable = false,
                    DisplayOrder = sourceOption.DisplayOrder,
                    MetadataJson = sourceOption.MetadataJson,
                    CreatedAt = now,
                    CreatedByAccountId = command.Scope.UserContext.AccountId
                });
            }
            product.OptionGroups.Add(clonedGroup);
        }

        await _products.AddProductAsync(product, cancellationToken);
        await _products.SaveChangesAsync(cancellationToken);
        return ApiResult<ProductResult>.Success(ProductResultMapper.ToResult(product), "Product template cloned.", 201);
    }
}
