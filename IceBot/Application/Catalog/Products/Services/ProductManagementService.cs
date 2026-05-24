using Application.Catalog.Abstractions;
using Application.Catalog.Products.Requests;
using Application.Catalog.Products.Results;
using Application.Shared.Wrappers;
using Domain.Catalog.Entities;
using Domain.Tenants.Enums;

namespace Application.Catalog.Products.Services;

public sealed class ProductManagementService
{
    private readonly IProductStore _products;

    public ProductManagementService(IProductStore products)
    {
        _products = products;
    }

    public async Task<PagedResult<ProductResult>> ListProductsAsync(
        string? search,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalCount = await _products.CountProductsAsync(search, organizationId, storeId, kioskId, cancellationToken);
        var products = await _products.ListProductsAsync(search, organizationId, storeId, kioskId, pageNumber, pageSize, cancellationToken);

        return PagedResult<ProductResult>.Success(
            products.Select(ToResult),
            totalCount,
            pageNumber,
            pageSize);
    }

    public async Task<ApiResult<ProductResult>> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetProductByIdAsync(productId, cancellationToken: cancellationToken);
        return product is null
            ? ApiResult<ProductResult>.Fail("Product not found.", 404)
            : ApiResult<ProductResult>.Success(ToResult(product));
    }

    public async Task<ApiResult<ProductResult>> CreateProductAsync(
        CreateProductRequest request,
        Guid? createdByAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var validationError = await ValidateCreateRequestAsync(request, cancellationToken);
        if (validationError is not null)
        {
            return ApiResult<ProductResult>.Fail(validationError);
        }

        var now = DateTimeOffset.UtcNow;
        var product = new Product
        {
            OrganizationId = request.OrganizationId,
            StoreId = request.StoreId,
            KioskId = request.KioskId,
            TemplateProductId = request.TemplateProductId,
            CategoryId = request.CategoryId,
            Code = NormalizeCode(request.Code),
            Name = request.Name.Trim(),
            DisplayName = TrimToNull(request.DisplayName),
            Description = TrimToNull(request.Description),
            ProductType = NormalizeOptionalCode(request.ProductType, "IceCream"),
            BasePrice = request.BasePrice,
            Currency = NormalizeOptionalCode(request.Currency, "VND"),
            IsAvailable = request.IsAvailable,
            PreparationTimeSeconds = request.PreparationTimeSeconds,
            ImageUrl = TrimToNull(request.ImageUrl),
            MetadataJson = TrimToNull(request.MetadataJson),
            ScopeType = request.ScopeType,
            CreatedAt = now,
            CreatedByAccountId = createdByAccountId
        };

        foreach (var variantRequest in request.Variants)
        {
            product.ProductVariants.Add(CreateVariant(variantRequest, product.Id, now, createdByAccountId));
        }

        await _products.AddProductAsync(product, cancellationToken);
        await _products.SaveChangesAsync(cancellationToken);

        return ApiResult<ProductResult>.Success(ToResult(product), "Product created.", 201);
    }

    public async Task<ApiResult<ProductResult>> UpdateProductAsync(
        Guid productId,
        UpdateProductRequest request,
        Guid? updatedByAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var product = await _products.GetProductByIdAsync(productId, asNoTracking: false, cancellationToken: cancellationToken);
        if (product is null)
        {
            return ApiResult<ProductResult>.Fail("Product not found.", 404);
        }

        var newCode = string.IsNullOrWhiteSpace(request.Code) ? product.Code : NormalizeCode(request.Code);
        var newOrganizationId = request.OrganizationId ?? product.OrganizationId;
        var newStoreId = request.StoreId ?? product.StoreId;
        var newKioskId = request.KioskId ?? product.KioskId;
        var newScopeType = request.ScopeType ?? product.ScopeType;

        var validationError = await ValidateProductFieldsAsync(
            newCode,
            request.Name ?? product.Name,
            request.BasePrice ?? product.BasePrice,
            request.Currency ?? product.Currency,
            request.PreparationTimeSeconds ?? product.PreparationTimeSeconds,
            newScopeType,
            newOrganizationId,
            newStoreId,
            newKioskId,
            request.CategoryId ?? product.CategoryId,
            productId,
            cancellationToken);

        if (validationError is not null)
        {
            return ApiResult<ProductResult>.Fail(validationError);
        }

        product.OrganizationId = newOrganizationId;
        product.StoreId = newStoreId;
        product.KioskId = newKioskId;
        product.TemplateProductId = request.TemplateProductId ?? product.TemplateProductId;
        product.CategoryId = request.CategoryId ?? product.CategoryId;
        product.Code = newCode;
        product.Name = string.IsNullOrWhiteSpace(request.Name) ? product.Name : request.Name.Trim();
        product.DisplayName = request.DisplayName is null ? product.DisplayName : TrimToNull(request.DisplayName);
        product.Description = request.Description is null ? product.Description : TrimToNull(request.Description);
        product.ProductType = string.IsNullOrWhiteSpace(request.ProductType)
            ? product.ProductType
            : NormalizeCode(request.ProductType);
        product.BasePrice = request.BasePrice ?? product.BasePrice;
        product.Currency = string.IsNullOrWhiteSpace(request.Currency)
            ? product.Currency
            : NormalizeCode(request.Currency);
        product.IsAvailable = request.IsAvailable ?? product.IsAvailable;
        product.PreparationTimeSeconds = request.PreparationTimeSeconds ?? product.PreparationTimeSeconds;
        product.ImageUrl = request.ImageUrl is null ? product.ImageUrl : TrimToNull(request.ImageUrl);
        product.MetadataJson = request.MetadataJson is null ? product.MetadataJson : TrimToNull(request.MetadataJson);
        product.ScopeType = newScopeType;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        product.UpdatedByAccountId = updatedByAccountId;

        await _products.SaveChangesAsync(cancellationToken);

        return ApiResult<ProductResult>.Success(ToResult(product), "Product updated.");
    }

    public async Task<ApiResult<ProductResult>> SetProductAvailabilityAsync(
        Guid productId,
        bool isAvailable,
        Guid? updatedByAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var product = await _products.GetProductByIdAsync(productId, asNoTracking: false, cancellationToken: cancellationToken);
        if (product is null)
        {
            return ApiResult<ProductResult>.Fail("Product not found.", 404);
        }

        product.IsAvailable = isAvailable;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        product.UpdatedByAccountId = updatedByAccountId;
        await _products.SaveChangesAsync(cancellationToken);

        return ApiResult<ProductResult>.Success(ToResult(product), "Product availability updated.");
    }

    public async Task<ApiResult<bool>> DeleteProductAsync(
        Guid productId,
        Guid? deletedByAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var product = await _products.GetProductByIdAsync(productId, asNoTracking: false, cancellationToken: cancellationToken);
        if (product is null)
        {
            return ApiResult<bool>.Fail("Product not found.", 404);
        }

        var now = DateTimeOffset.UtcNow;
        product.DeletedAt = now;
        product.DeletedByAccountId = deletedByAccountId;

        foreach (var variant in product.ProductVariants)
        {
            variant.DeletedAt = now;
            variant.DeletedByAccountId = deletedByAccountId;
        }

        await _products.SaveChangesAsync(cancellationToken);
        return ApiResult<bool>.Success(true, "Product deleted.");
    }

    public async Task<ApiResult<ProductVariantResult>> AddVariantAsync(
        Guid productId,
        UpsertProductVariantRequest request,
        Guid? createdByAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var product = await _products.GetProductByIdAsync(productId, asNoTracking: false, cancellationToken: cancellationToken);
        if (product is null)
        {
            return ApiResult<ProductVariantResult>.Fail("Product not found.", 404);
        }

        var validationError = await ValidateVariantFieldsAsync(productId, request, null, cancellationToken);
        if (validationError is not null)
        {
            return ApiResult<ProductVariantResult>.Fail(validationError);
        }

        var variant = CreateVariant(request, product.Id, DateTimeOffset.UtcNow, createdByAccountId);
        await _products.AddProductVariantAsync(variant, cancellationToken);
        await _products.SaveChangesAsync(cancellationToken);

        return ApiResult<ProductVariantResult>.Success(ToVariantResult(variant), "Product variant created.", 201);
    }

    public async Task<ApiResult<ProductVariantResult>> UpdateVariantAsync(
        Guid productId,
        Guid variantId,
        UpdateProductVariantRequest request,
        Guid? updatedByAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var variant = await _products.GetProductVariantByIdAsync(productId, variantId, asNoTracking: false, cancellationToken: cancellationToken);
        if (variant is null)
        {
            return ApiResult<ProductVariantResult>.Fail("Product variant not found.", 404);
        }

        var newCode = string.IsNullOrWhiteSpace(request.Code) ? variant.Code : NormalizeCode(request.Code);
        if (await _products.ProductVariantCodeExistsAsync(productId, newCode, variantId, cancellationToken))
        {
            return ApiResult<ProductVariantResult>.Fail("Product variant code already exists for this product.", 409);
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            variant.Name = request.Name.Trim();
        }

        variant.Code = newCode;
        variant.DisplayName = request.DisplayName is null ? variant.DisplayName : TrimToNull(request.DisplayName);
        variant.Description = request.Description is null ? variant.Description : TrimToNull(request.Description);
        variant.VariantType = string.IsNullOrWhiteSpace(request.VariantType)
            ? variant.VariantType
            : NormalizeCode(request.VariantType);
        variant.SizeCode = request.SizeCode is null ? variant.SizeCode : NormalizeNullableCode(request.SizeCode);
        variant.BasePrice = request.BasePrice ?? variant.BasePrice;
        variant.Currency = string.IsNullOrWhiteSpace(request.Currency)
            ? variant.Currency
            : NormalizeCode(request.Currency);
        variant.IsAvailable = request.IsAvailable ?? variant.IsAvailable;
        variant.DisplayOrder = request.DisplayOrder ?? variant.DisplayOrder;
        variant.PreparationTimeSeconds = request.PreparationTimeSeconds ?? variant.PreparationTimeSeconds;
        variant.ImageUrl = request.ImageUrl is null ? variant.ImageUrl : TrimToNull(request.ImageUrl);
        variant.MetadataJson = request.MetadataJson is null ? variant.MetadataJson : TrimToNull(request.MetadataJson);
        variant.UpdatedAt = DateTimeOffset.UtcNow;
        variant.UpdatedByAccountId = updatedByAccountId;

        var validationError = ValidateVariantValues(
            variant.Code,
            variant.Name,
            variant.BasePrice,
            variant.Currency,
            variant.PreparationTimeSeconds);
        if (validationError is not null)
        {
            return ApiResult<ProductVariantResult>.Fail(validationError);
        }

        await _products.SaveChangesAsync(cancellationToken);
        return ApiResult<ProductVariantResult>.Success(ToVariantResult(variant), "Product variant updated.");
    }

    public async Task<ApiResult<ProductVariantResult>> SetVariantAvailabilityAsync(
        Guid productId,
        Guid variantId,
        bool isAvailable,
        Guid? updatedByAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var variant = await _products.GetProductVariantByIdAsync(productId, variantId, asNoTracking: false, cancellationToken: cancellationToken);
        if (variant is null)
        {
            return ApiResult<ProductVariantResult>.Fail("Product variant not found.", 404);
        }

        variant.IsAvailable = isAvailable;
        variant.UpdatedAt = DateTimeOffset.UtcNow;
        variant.UpdatedByAccountId = updatedByAccountId;
        await _products.SaveChangesAsync(cancellationToken);

        return ApiResult<ProductVariantResult>.Success(ToVariantResult(variant), "Product variant availability updated.");
    }

    public async Task<ApiResult<bool>> DeleteVariantAsync(
        Guid productId,
        Guid variantId,
        Guid? deletedByAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var variant = await _products.GetProductVariantByIdAsync(productId, variantId, asNoTracking: false, cancellationToken: cancellationToken);
        if (variant is null)
        {
            return ApiResult<bool>.Fail("Product variant not found.", 404);
        }

        variant.DeletedAt = DateTimeOffset.UtcNow;
        variant.DeletedByAccountId = deletedByAccountId;
        await _products.SaveChangesAsync(cancellationToken);

        return ApiResult<bool>.Success(true, "Product variant deleted.");
    }

    private async Task<string?> ValidateCreateRequestAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var validationError = await ValidateProductFieldsAsync(
            request.Code,
            request.Name,
            request.BasePrice,
            request.Currency,
            request.PreparationTimeSeconds,
            request.ScopeType,
            request.OrganizationId,
            request.StoreId,
            request.KioskId,
            request.CategoryId,
            null,
            cancellationToken);

        if (validationError is not null)
        {
            return validationError;
        }

        var variantCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var variant in request.Variants)
        {
            var variantError = ValidateVariantValues(
                variant.Code,
                variant.Name,
                variant.BasePrice,
                variant.Currency,
                variant.PreparationTimeSeconds);

            if (variantError is not null)
            {
                return variantError;
            }

            if (!variantCodes.Add(NormalizeCode(variant.Code)))
            {
                return $"Duplicate variant code '{variant.Code}'.";
            }
        }

        return null;
    }

    private async Task<string?> ValidateProductFieldsAsync(
        string code,
        string name,
        decimal basePrice,
        string currency,
        int? preparationTimeSeconds,
        TenantScopeType scopeType,
        Guid? organizationId,
        Guid? storeId,
        Guid? kioskId,
        long? categoryId,
        Guid? excludedProductId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return "Product code is required.";
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return "Product name is required.";
        }

        if (basePrice < 0)
        {
            return "Product base price cannot be negative.";
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return "Currency is required.";
        }

        if (preparationTimeSeconds < 0)
        {
            return "Preparation time cannot be negative.";
        }

        var scopeError = ValidateTenantScope(scopeType, organizationId, storeId, kioskId);
        if (scopeError is not null)
        {
            return scopeError;
        }

        if (categoryId.HasValue && !await _products.ProductCategoryExistsAsync(categoryId.Value, cancellationToken))
        {
            return "Product category does not exist.";
        }

        if (await _products.ProductCodeExistsAsync(
            organizationId,
            storeId,
            kioskId,
            NormalizeCode(code),
            excludedProductId,
            cancellationToken))
        {
            return "Product code already exists in this scope.";
        }

        return null;
    }

    private async Task<string?> ValidateVariantFieldsAsync(
        Guid productId,
        UpsertProductVariantRequest request,
        Guid? excludedVariantId,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateVariantValues(
            request.Code,
            request.Name,
            request.BasePrice,
            request.Currency,
            request.PreparationTimeSeconds);

        if (validationError is not null)
        {
            return validationError;
        }

        if (await _products.ProductVariantCodeExistsAsync(productId, NormalizeCode(request.Code), excludedVariantId, cancellationToken))
        {
            return "Product variant code already exists for this product.";
        }

        return null;
    }

    private static string? ValidateVariantValues(
        string code,
        string name,
        decimal basePrice,
        string currency,
        int? preparationTimeSeconds)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return "Product variant code is required.";
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return "Product variant name is required.";
        }

        if (basePrice < 0)
        {
            return "Product variant base price cannot be negative.";
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            return "Product variant currency is required.";
        }

        if (preparationTimeSeconds < 0)
        {
            return "Product variant preparation time cannot be negative.";
        }

        return null;
    }

    private static string? ValidateTenantScope(TenantScopeType scopeType, Guid? organizationId, Guid? storeId, Guid? kioskId)
    {
        return scopeType switch
        {
            TenantScopeType.Global when organizationId is not null || storeId is not null || kioskId is not null =>
                "Global product cannot be assigned to organization, store, or kiosk.",
            TenantScopeType.Organization when organizationId is null || storeId is not null || kioskId is not null =>
                "Organization-scoped product requires organizationId only.",
            TenantScopeType.Store when organizationId is null || storeId is null || kioskId is not null =>
                "Store-scoped product requires organizationId and storeId only.",
            TenantScopeType.Kiosk when organizationId is null || storeId is null || kioskId is null =>
                "Kiosk-scoped product requires organizationId, storeId, and kioskId.",
            TenantScopeType.Device => "Device-scoped product is not supported.",
            _ => null
        };
    }

    private static ProductVariant CreateVariant(
        UpsertProductVariantRequest request,
        Guid productId,
        DateTimeOffset now,
        Guid? createdByAccountId)
    {
        return new ProductVariant
        {
            ProductId = productId,
            Code = NormalizeCode(request.Code),
            Name = request.Name.Trim(),
            DisplayName = TrimToNull(request.DisplayName),
            Description = TrimToNull(request.Description),
            VariantType = NormalizeOptionalCode(request.VariantType, "Default"),
            SizeCode = NormalizeNullableCode(request.SizeCode),
            BasePrice = request.BasePrice,
            Currency = NormalizeOptionalCode(request.Currency, "VND"),
            IsAvailable = request.IsAvailable,
            DisplayOrder = request.DisplayOrder,
            PreparationTimeSeconds = request.PreparationTimeSeconds,
            ImageUrl = TrimToNull(request.ImageUrl),
            MetadataJson = TrimToNull(request.MetadataJson),
            CreatedAt = now,
            CreatedByAccountId = createdByAccountId
        };
    }

    private static ProductResult ToResult(Product product)
    {
        return new ProductResult
        {
            Id = product.Id,
            OrganizationId = product.OrganizationId,
            StoreId = product.StoreId,
            KioskId = product.KioskId,
            TemplateProductId = product.TemplateProductId,
            CategoryId = product.CategoryId,
            Code = product.Code,
            Name = product.Name,
            DisplayName = product.DisplayName,
            Description = product.Description,
            ProductType = product.ProductType,
            BasePrice = product.BasePrice,
            Currency = product.Currency,
            IsAvailable = product.IsAvailable,
            PreparationTimeSeconds = product.PreparationTimeSeconds,
            ImageUrl = product.ImageUrl,
            MetadataJson = product.MetadataJson,
            ScopeType = product.ScopeType,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            Variants = product.ProductVariants
                .OrderBy(variant => variant.DisplayOrder)
                .ThenBy(variant => variant.Name)
                .Select(ToVariantResult)
                .ToList()
        };
    }

    private static ProductVariantResult ToVariantResult(ProductVariant variant)
    {
        return new ProductVariantResult
        {
            Id = variant.Id,
            ProductId = variant.ProductId,
            Code = variant.Code,
            Name = variant.Name,
            DisplayName = variant.DisplayName,
            Description = variant.Description,
            VariantType = variant.VariantType,
            SizeCode = variant.SizeCode,
            BasePrice = variant.BasePrice,
            Currency = variant.Currency,
            IsAvailable = variant.IsAvailable,
            DisplayOrder = variant.DisplayOrder,
            PreparationTimeSeconds = variant.PreparationTimeSeconds,
            ImageUrl = variant.ImageUrl,
            MetadataJson = variant.MetadataJson,
            CreatedAt = variant.CreatedAt,
            UpdatedAt = variant.UpdatedAt
        };
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();

    private static string NormalizeOptionalCode(string? value, string defaultValue)
    {
        return string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : NormalizeCode(value);
    }

    private static string? NormalizeNullableCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : NormalizeCode(value);
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
